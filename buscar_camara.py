import cv2

print("Buscando cámaras conectadas...")
for i in range(10):  # Probará los puertos del 0 al 9
    cap = cv2.VideoCapture(i)
    if cap.isOpened():
        success, frame = cap.read()
        if success:
            print(f"✅ ¡Cámara detectada en el índice: {i}!")
            # Mostrar la cámara por un segundo para que veas cuál es
            cv2.imshow(f"Probando Camara {i}", frame)
            cv2.waitKey(1500) # Muestra la ventana por 1.5 segundos
            cv2.destroyAllWindows()
    cap.release()
print("Búsqueda terminada.")
