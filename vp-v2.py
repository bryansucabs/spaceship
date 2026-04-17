import cv2
import numpy as np
import socket
import json
import math

# ==========================================
# --- CONFIGURACIÓN PRINCIPAL ---
# ==========================================
# 1. Cámara: Recuerda que usaste el índice 2 para tu webcam externa.
CAMERA_INDEX = 2

# 2. Red: Dirección IP y Puerto de Unity.
UNITY_IP = "127.0.0.1" 
UNITY_PORT = 5002

# 3. Matemáticas de Control: V = k * (e^(alpha*d) - 1).
K_COEFF = 50.0      # Multiplicador base (Velocidad máxima).
ALPHA_COEFF = 1.5   # Agresividad de la curva exponencial.
DEADZONE = 0.05     # Margen del 5% desde la base donde no hay movimiento.

# ==========================================
# --- CONFIGURACIÓN DE COLORES (HSV) ---
# ==========================================
# En OpenCV, los rangos HSV son: H (0-179), S (0-255), V (0-255)
# Estos son valores aproximados. Si no detecta bien, necesitarás ajustarlos
# según la iluminación de tu habitación.

# PIE DERECHO (Acelerador) -> Buscando VERDE
COLOR_RIGHT_LOWER = np.array([35, 100, 50])
COLOR_RIGHT_UPPER = np.array([85, 255, 255])

# PIE IZQUIERDO (Freno) -> Buscando AZUL
COLOR_LEFT_LOWER = np.array([100, 150, 50])
COLOR_LEFT_UPPER = np.array([140, 255, 255])

# Tamaño mínimo en píxeles para que no confunda "basura" visual con tu pie
MIN_BLOB_AREA = 300 

# ==========================================
# --- INICIALIZACIÓN ---
# ==========================================
sock = socket.socket(socket.AF_INET, socket.SOCK_DGRAM)

calibrated_corners = []
calibrating = True
warped_width, warped_height = 300, 400 

def mouse_callback(event, x, y, flags, param):
    """Maneja los clics para definir las esquinas del tapete."""
    global calibrated_corners, calibrating
    if calibrating and event == cv2.EVENT_LBUTTONDOWN:
        calibrated_corners.append((x, y))
        print(f"Esquina capturada: ({x}, {y})")
        if len(calibrated_corners) == 4:
            calibrating = False
            print("--- Calibración Completada. Iniciando Detección por Color ---")

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
print("1. Coloca la cámara para que vea el tapete y tus pies (puedes estar sentado).")
print("2. Usa calcetines/stickers VERDES en el pie derecho y AZULES en el izquierdo.")
print("3. Haz clic en las 4 esquinas del tapete en la imagen.")
print("4. ORDEN: Arriba-Izquierda -> Arriba-Derecha -> Abajo-Derecha -> Abajo-Izquierda.")

while cap.isOpened():
    success, image = cap.read()
    if not success:
        continue

    # 1. MODO CALIBRACIÓN
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

    # 2. PROCESAMIENTO DE VISIÓN (Post-Calibración)
    src_pts = np.float32(calibrated_corners)
    dst_pts = np.float32([[0, 0], [warped_width, 0], [warped_width, warped_height], [0, warped_height]])
    M = cv2.getPerspectiveTransform(src_pts, dst_pts)
    warped_image = cv2.warpPerspective(image, M, (warped_width, warped_height))

    # Convertir a espacio de color HSV para hacer el seguimiento
    warped_hsv = cv2.cvtColor(warped_image, cv2.COLOR_BGR2HSV)

    control_data = {"accel": 0.0, "brake": 0.0}

    # --- SEGUIMIENTO DERECHO (ACELERADOR / VERDE) ---
    mask_right = cv2.inRange(warped_hsv, COLOR_RIGHT_LOWER, COLOR_RIGHT_UPPER)
    contours_right, _ = cv2.findContours(mask_right, cv2.RETR_EXTERNAL, cv2.CHAIN_APPROX_SIMPLE)
    
    if contours_right:
        largest_contour = max(contours_right, key=cv2.contourArea)
        if cv2.contourArea(largest_contour) > MIN_BLOB_AREA:
            # Encontrar el centroide del color
            Moments = cv2.moments(largest_contour)
            if Moments['m00'] != 0:
                cx = int(Moments['m10']/Moments['m00'])
                cy = int(Moments['m01']/Moments['m00'])
                
                # Dibujar un círculo verde en la vista warped para confirmar
                cv2.circle(warped_image, (cx, cy), 15, (0, 255, 0), -1)

                # Calcular aceleración
                if 0 < cy < warped_height:
                    d_accel = (warped_height - cy) / warped_height
                    if d_accel > DEADZONE:
                        normalized_d = (d_accel - DEADZONE) / (1.0 - DEADZONE)
                        control_data["accel"] = K_COEFF * (math.exp(ALPHA_COEFF * normalized_d) - 1)

    # --- SEGUIMIENTO IZQUIERDO (FRENO / AZUL) ---
    mask_left = cv2.inRange(warped_hsv, COLOR_LEFT_LOWER, COLOR_LEFT_UPPER)
    contours_left, _ = cv2.findContours(mask_left, cv2.RETR_EXTERNAL, cv2.CHAIN_APPROX_SIMPLE)
    
    if contours_left:
        largest_contour = max(contours_left, key=cv2.contourArea)
        if cv2.contourArea(largest_contour) > MIN_BLOB_AREA:
            # Encontrar el centroide del color
            Moments = cv2.moments(largest_contour)
            if Moments['m00'] != 0:
                cx = int(Moments['m10']/Moments['m00'])
                cy = int(Moments['m01']/Moments['m00'])
                
                # Dibujar un círculo azul en la vista warped para confirmar
                cv2.circle(warped_image, (cx, cy), 15, (255, 0, 0), -1)

                # Calcular freno
                if 0 < cy < warped_height:
                    d_brake = (warped_height - cy) / warped_height
                    if d_brake > DEADZONE:
                        normalized_d = (d_brake - DEADZONE) / (1.0 - DEADZONE)
                        control_data["brake"] = K_COEFF * (math.exp(ALPHA_COEFF * normalized_d) - 1)

    # 3. ENVIAR DATOS A UNITY
    message = json.dumps(control_data).encode('utf-8')
    sock.sendto(message, (UNITY_IP, UNITY_PORT))

    # --- VISUALIZACIÓN DEBUG ---
    # Líneas de la zona muerta y el punto cero
    cv2.line(warped_image, (0, warped_height), (warped_width, warped_height), (0, 0, 255), 3)
    cv2.line(warped_image, (0, int(warped_height*(1-DEADZONE))), (warped_width, int(warped_height*(1-DEADZONE))), (100, 100, 255), 1)

    # Textos de valores
    cv2.putText(warped_image, f"Acel: {control_data['accel']:.1f}", (10, 30), cv2.FONT_HERSHEY_SIMPLEX, 0.8, (0, 255, 0), 2)
    cv2.putText(warped_image, f"Freno: {control_data['brake']:.1f}", (10, 60), cv2.FONT_HERSHEY_SIMPLEX, 0.8, (255, 0, 0), 2)

    # Para ver qué está detectando internamente OpenCV (opcional, puedes comentar estas dos líneas luego)
    cv2.imshow('Mascara Derecha (Verde)', mask_right)
    cv2.imshow('Mascara Izquierda (Azul)', mask_left)

    cv2.imshow('Vision de Pies (Camara)', image)
    cv2.imshow('Vista Tapete Planeada', warped_image)

    if cv2.waitKey(1) & 0xFF == 27:
        break

cap.release()
cv2.destroyAllWindows()


