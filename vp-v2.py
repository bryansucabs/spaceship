import cv2
import numpy as np
import socket
import json
import math
import time

# ==========================================
# --- CONFIGURACIÓN PRINCIPAL ---
# ==========================================
CAMERA_INDEX = 2
UNITY_IP = "127.0.0.1"
UNITY_PORT = 5002

K_COEFF = 50.0
ALPHA_COEFF = 1.5
DEADZONE = 0.05

# ==========================================
# --- CONFIGURACIÓN DE COLORES (HSV) ---
# ==========================================
# PIE DERECHO (Acelerador / Dirección) -> VERDE
COLOR_RIGHT_LOWER = np.array([35, 100, 50])
COLOR_RIGHT_UPPER = np.array([85, 255, 255])

# PIE IZQUIERDO (Retroceso) -> AZUL
COLOR_LEFT_LOWER = np.array([100, 150, 50])
COLOR_LEFT_UPPER = np.array([140, 255, 255])

MIN_BLOB_AREA = 300

# Segundos sin ver el pie DERECHO antes de activar la alerta
FOOT_LOST_TIMEOUT = 1.5

# ==========================================
# --- INICIALIZACIÓN ---
# ==========================================
sock = socket.socket(socket.AF_INET, socket.SOCK_DGRAM)

calibrated_corners = []
calibrating = True
warped_width, warped_height = 300, 400

last_seen_right = time.time()   # Solo rastreamos el pie derecho para la alerta

def mouse_callback(event, x, y, flags, param):
    global calibrated_corners, calibrating
    if calibrating and event == cv2.EVENT_LBUTTONDOWN:
        calibrated_corners.append((x, y))
        print(f"Esquina capturada: ({x}, {y})")
        if len(calibrated_corners) == 4:
            calibrating = False
            print("--- Calibración Completada. Iniciando Detección ---")

# ==========================================
# --- FLUJO PRINCIPAL ---
# ==========================================
cap = cv2.VideoCapture(CAMERA_INDEX)
if not cap.isOpened():
    print(f"Error: No se puede abrir la cámara {CAMERA_INDEX}.")
    exit()

cv2.namedWindow('Vision de Pies (Camara)')
cv2.setMouseCallback('Vision de Pies (Camara)', mouse_callback)

print("\n--- PASO DE CALIBRACIÓN ---")
print("1. Coloca la cámara para que vea el tapete y tus pies.")
print("2. Pie DERECHO  = calcetín/sticker VERDE  → acelera hacia adelante")
print("3. Pie IZQUIERDO = calcetín/sticker AZUL  → activa retroceso")
print("   (cuando el azul está activo, la aceleración del verde va hacia atrás)")
print("4. Haz clic en las 4 esquinas del tapete.")
print("   ORDEN: Arriba-Izq → Arriba-Der → Abajo-Der → Abajo-Izq")

while cap.isOpened():
    success, image = cap.read()
    if not success:
        continue

    # ---- MODO CALIBRACIÓN ----
    if calibrating:
        for point in calibrated_corners:
            cv2.circle(image, point, 5, (0, 0, 255), -1)
        if len(calibrated_corners) < 4:
            cv2.putText(image, f"Clic en Esquina {len(calibrated_corners)+1}/4", (10, 30),
                        cv2.FONT_HERSHEY_SIMPLEX, 0.7, (0, 0, 255), 2)
        cv2.imshow('Vision de Pies (Camara)', image)
        if cv2.waitKey(1) & 0xFF == 27:
            break
        continue

    # ---- PROCESAMIENTO POST-CALIBRACIÓN ----
    src_pts = np.float32(calibrated_corners)
    dst_pts = np.float32([[0, 0], [warped_width, 0], [warped_width, warped_height], [0, warped_height]])
    M = cv2.getPerspectiveTransform(src_pts, dst_pts)
    warped_image = cv2.warpPerspective(image, M, (warped_width, warped_height))
    warped_hsv   = cv2.cvtColor(warped_image, cv2.COLOR_BGR2HSV)

    now = time.time()

    # -----------------------------------------------------------
    # control_data:
    #   accel            → magnitud de velocidad (calculada con el pie verde)
    #   reverse          → 1 si el pie azul está activo, 0 si no
    #                      La nave usará 'accel' hacia atrás cuando reverse=1
    #   foot_right_lost  → 1 si el pie derecho lleva más de FOOT_LOST_TIMEOUT sin verse
    # -----------------------------------------------------------
    control_data = {
        "accel":           0.0,
        "reverse":         0,      # bandera booleana: 0=adelante, 1=atrás
        "foot_right_lost": 0
    }

    # ---- PIE IZQUIERDO: RETROCESO (AZUL) — se detecta primero para saber el modo ----
    mask_left = cv2.inRange(warped_hsv, COLOR_LEFT_LOWER, COLOR_LEFT_UPPER)
    contours_left, _ = cv2.findContours(mask_left, cv2.RETR_EXTERNAL, cv2.CHAIN_APPROX_SIMPLE)
    reverse_active = False

    if contours_left:
        largest = max(contours_left, key=cv2.contourArea)
        if cv2.contourArea(largest) > MIN_BLOB_AREA:
            M_mom = cv2.moments(largest)
            if M_mom['m00'] != 0:
                cx = int(M_mom['m10'] / M_mom['m00'])
                cy = int(M_mom['m01'] / M_mom['m00'])
                cv2.circle(warped_image, (cx, cy), 15, (255, 100, 0), -1)
                reverse_active = True
                control_data["reverse"] = 1

    # ---- PIE DERECHO: ACELERACIÓN (VERDE) ----
    mask_right = cv2.inRange(warped_hsv, COLOR_RIGHT_LOWER, COLOR_RIGHT_UPPER)
    contours_right, _ = cv2.findContours(mask_right, cv2.RETR_EXTERNAL, cv2.CHAIN_APPROX_SIMPLE)
    right_detected = False

    if contours_right:
        largest = max(contours_right, key=cv2.contourArea)
        if cv2.contourArea(largest) > MIN_BLOB_AREA:
            M_mom = cv2.moments(largest)
            if M_mom['m00'] != 0:
                cx = int(M_mom['m10'] / M_mom['m00'])
                cy = int(M_mom['m01'] / M_mom['m00'])
                cv2.circle(warped_image, (cx, cy), 15, (0, 255, 0), -1)
                right_detected = True
                last_seen_right = now

                if 0 < cy < warped_height:
                    d = (warped_height - cy) / warped_height
                    if d > DEADZONE:
                        nd = (d - DEADZONE) / (1.0 - DEADZONE)
                        control_data["accel"] = K_COEFF * (math.exp(ALPHA_COEFF * nd) - 1)

    # ---- ALERTA: solo si el pie DERECHO está fuera de la cámara ----
    if not right_detected and (now - last_seen_right) > FOOT_LOST_TIMEOUT:
        control_data["foot_right_lost"] = 1

    # ---- ENVIAR A UNITY ----
    message = json.dumps(control_data).encode('utf-8')
    sock.sendto(message, (UNITY_IP, UNITY_PORT))

    # ---- VISUALIZACIÓN DEBUG ----
    cv2.line(warped_image, (0, warped_height), (warped_width, warped_height), (0, 0, 255), 3)
    cv2.line(warped_image,
             (0, int(warped_height * (1 - DEADZONE))),
             (warped_width, int(warped_height * (1 - DEADZONE))),
             (100, 100, 255), 1)

    modo = "RETROCESO" if reverse_active else "ADELANTE"
    color_modo = (255, 120, 0) if reverse_active else (0, 220, 0)
    cv2.putText(warped_image, f"Modo:  {modo}",                        (10, 25),  cv2.FONT_HERSHEY_SIMPLEX, 0.65, color_modo,   2)
    cv2.putText(warped_image, f"Acel:  {control_data['accel']:.1f}",   (10, 55),  cv2.FONT_HERSHEY_SIMPLEX, 0.65, (0, 220, 0),  2)

    if control_data["foot_right_lost"]:
        cv2.putText(warped_image, "! PIE DER FUERA", (10, 90), cv2.FONT_HERSHEY_SIMPLEX, 0.6, (0, 0, 255), 2)

    cv2.imshow('Mascara Derecha (Verde)', mask_right)
    cv2.imshow('Mascara Izquierda (Azul)', mask_left)
    cv2.imshow('Vision de Pies (Camara)', image)
    cv2.imshow('Vista Tapete Planeada', warped_image)

    if cv2.waitKey(1) & 0xFF == 27:
        break

cap.release()
cv2.destroyAllWindows()