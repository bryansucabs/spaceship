# Spaceship Tunnel Game

Juego de vuelo en primera/tercera persona desarrollado en Unity 6.
La nave avanza automaticamente por un tunel octagonal mientras el jugador
esquiva obstaculos de diferentes tipos y colores con las teclas de direccion.

---

## Requisitos del sistema

| Requisito | Version |
|-----------|---------|
| Unity Hub | Cualquier version reciente |
| Unity Editor | **6000.4.0f1** (exactamente esta version) |
| Pipeline de render | Universal Render Pipeline (URP) — ya incluido |
| Input System | New Input System — ya incluido |

> **IMPORTANTE:** todos deben usar exactamente **Unity 6000.4.0f1**.
> Abrirlo con otra version puede romper materiales o scripts.
> En Unity Hub al agregar el proyecto aparece la version requerida automaticamente.

---

## Assets de la Asset Store que hay que descargar (gratis)

Estos assets no estan incluidos en el repositorio por su tamano.
Hay que descargarlos desde la Asset Store de Unity e importarlos al proyecto.

### 1. StarSparrow Modular Spaceship (la nave)
- Buscar en la Asset Store: **"StarSparrow Modular Spaceship"**
- Autor: **3DRT**
- Es gratuito
- Importar todo el paquete al proyecto
- Link: https://assetstore.unity.com/packages/3d/vehicles/space/star-sparrow-modular-spaceship-73167?srsltid=AfmBOoqV3ANyh_PusS-NRw5pqmKIwBxaIjEYdWSCVIyabQHT_86VyrD0

### 2. Vibrant 4K Starfield Skybox Pack (el fondo de estrellas)
- Buscar en la Asset Store: **"Vibrant 4K Starfield Skybox Pack"**
- Autor: **Parallel Cascades**
- Es gratuito
- Importar todo el paquete al proyecto
- link: https://assetstore.unity.com/packages/2d/textures-materials/sky/vibrant-4k-starfield-skybox-pack-292597?srsltid=AfmBOopoPwQzmiRo7NWby8eegBs45qjStM47CFdAoZFFB7BKrkkWWZ9V

> Sin estos dos assets el proyecto abre pero la nave no se ve
> y el fondo queda negro o en color solido.

---

## Pasos para configurar el proyecto (desde cero)

### Paso 1 — Clonar o descargar el repositorio

**Opcion A — Con Git:**
```
git clone https://github.com/TU-USUARIO/TU-REPO.git
```

**Opcion B — Sin Git:**
- En GitHub hacer clic en el boton verde **"Code"**
- Seleccionar **"Download ZIP"**
- Descomprimir la carpeta en donde quieras

---

### Paso 2 — Agregar el proyecto a Unity Hub

1. Abrir **Unity Hub**
2. Ir a la seccion **"Projects"**
3. Clic en **"Add"** → **"Add project from disk"**
4. Seleccionar la carpeta del proyecto (la que contiene Assets/, ProjectSettings/, etc.)
5. Unity Hub detecta automaticamente que necesita la version **6000.4.0f1**
   - Si no la tienes instalada, Unity Hub te ofrece instalarla

---

### Paso 3 — Descargar e importar los assets

1. Abrir Unity con el proyecto
2. Ir a **Window → Asset Store** (o abrir unity.com/asset-store en el navegador)
3. Buscar e importar **StarSparrow Modular Spaceship**
4. Buscar e importar **Vibrant 4K Starfield Skybox Pack**

---

### Paso 4 — Configurar la nave en la escena

Despues de importar StarSparrow:

1. En la carpeta del proyecto ir a: `Assets/StarSparrow/Prefabs/`
2. Buscar el prefab **StarSparrow1**
3. Arrastrar **StarSparrow1** a la Hierarchy de Unity
4. Renombrarlo a **PlayerShip** (clic derecho → Rename)
5. En el Inspector asegurarse de que la posicion sea **(0, 0, 0)**
6. Agregar los scripts **ShipController** y **ShipHealth** al PlayerShip
   (clic en "Add Component" y buscar cada uno)

---

### Paso 5 — Configurar el Skybox

1. Ir a **Window → Rendering → Lighting**
2. En la seccion **Environment → Skybox Material**
3. Hacer clic en el circulo a la derecha y seleccionar uno de los materiales
   de la carpeta `Assets/ParallelCascades/Vibrant 4K Starfield Skybox Pack/`

---

### Paso 6 — Construir el tunel y los obstaculos

1. En la barra de menus de Unity ir a **Tools → Build Full Scene**
2. Esperar a que termine (puede tardar unos segundos)
3. En la Hierarchy deben aparecer dos objetos nuevos: **Tunnel** y **Obstacles**
4. Guardar la escena con **Ctrl+S**

---

### Paso 7 — Probar el juego

1. Presionar el boton **Play** (triangulo) en Unity
2. La nave avanza sola por el tunel
3. Usar las teclas para esquivar obstaculos

---

## Controles

| Tecla | Accion |
|-------|--------|
| Flecha izquierda / A | Mover la nave a la izquierda |
| Flecha derecha / D | Mover la nave a la derecha |
| Flecha arriba / W | Mover la nave hacia arriba |
| Flecha abajo / S | Mover la nave hacia abajo |
| R | Reiniciar el juego (cuando termina) |

---

## Descripcion de los obstaculos

El juego tiene 9 tipos de obstaculos, cada uno con un color distinto:

| Color | Tipo | Descripcion |
|-------|------|-------------|
| Naranja | Mitad horizontal | Losa que nace de la pared izquierda o derecha |
| Rojo | Mitad vertical | Losa que nace de la pared superior o inferior |
| Verde lima | Diagonal | Dos barras desde paredes diagonales opuestas |
| Cyan | Tres cuartos | Solo una esquina libre, el resto bloqueado |
| Violeta | Doble mitad | Dos losas desde paredes opuestas, hueco en el centro |
| Amarillo | Cruz | Cuatro barras desde las 4 paredes, hueco en el centro |
| Turquesa | Zigzag | Diagonal seguida de una mitad a 60 unidades de distancia |
| Verde brillante | Puerta | Barra completa con hueco en posicion aleatoria |
| Azul claro | Corredor diagonal | Dos barras paralelas, hay que pasar entre ellas en diagonal |

---

## Estructura del proyecto

```
Assets/
├── Editor/
│   └── GameSceneBuilder.cs   — menu Tools > Build Full Scene
├── Scenes/
│   └── SampleScene.unity     — la escena principal del juego
├── Scripts/
│   ├── ShipController.cs     — movimiento de la nave
│   ├── ShipHealth.cs         — vida y cooldown de dano
│   ├── CameraFollow.cs       — camara que sigue a la nave
│   ├── GameManager.cs        — temporizador, UI, reinicio
│   ├── TunnelGenerator.cs    — generador de tunel (fallback en runtime)
│   ├── TunnelWall.cs         — detecta colisiones y genera chispas
│   ├── SparkEffect.cs        — efecto de particulas al chocar
│   ├── ObstacleBuilder.cs    — logica de construccion de los 9 tipos de obstaculos
│   └── ObstacleGenerator.cs  — generador de obstaculos (fallback en runtime)
└── Settings/
    └── ...                   — configuracion de URP
```

---

## Mecanicas principales

- **Avance automatico:** la nave se mueve sola hacia adelante a 120 unidades/segundo
- **5 vidas:** cada colision con pared u obstaculo quita 1 vida (con cooldown de 0.4s)
- **Chispas:** al chocar aparecen particulas en el punto de contacto
- **Temporizador:** el juego dura 2 minutos, si sobrevives ganas
- **22 obstaculos** distribuidos por todo el tunel con variedad de tipos y colores

---

## Notas

- Si al abrir el proyecto la escena se ve vacia o sin tunel, ejecutar **Tools → Build Full Scene**
- El tunel esta pre-construido en Edit Mode para que se vea antes de presionar Play
- Los limites de movimiento de la nave se corrigen automaticamente al iniciar Play
- El GameObject **AsteroidSpawner** que puede aparecer en la Hierarchy puede borrarse, no se usa


# Segunda Parte (conectar camara con juego)

## 2. entorno virtual
sudo apt install python3-venv python3-pip
### 2.1 crear carpeta donde se colocara en entorno virtual
mkdir ~/ProyectoNave && cd ~/ProyectoNave
python3 -m venv venv

### 2.2 activar entorno virtual, hay dos formas
a) source venv/bin/activate

b) source ~/ProyectoNave/venv/bin/activate

### 2.3 instalar librerias dentro del entorno virtual
pip install opencv-python numpy

pip install mediapipe==0.10.14

## Conectar todo en el Inspector

- Selecciona tu objeto Nave (o StarSparrow, el que estés usando) en la jerarquía de Unity.
- Arrastra el nuevo script UDPReceiver desde tu carpeta y suéltalo sobre el Inspector de la Nave para añadírselo como componente.
- Ahora, en la Nave, busca el componente Ship Controller, verás que tiene una casilla vacía llamada Receptor UDP.
- Como el UDPReceiver ahora vive en la misma Nave, solo haz clic izquierdo en el título del componente UDPReceiver, arrástralo y suéltalo dentro de esa casilla vacía del Ship Controller.

¡Eso es todo! Guarda tu escena (Ctrl + S). Primero ejecuta tu script de Python (python vp-v2.py) desde tu terminal de Ubuntu y luego dale al botón de Play en Unity.

## correr todo
cd ProyectoNave && source venv/bin/activate && python vp-v2.py
- Lo primero que hace es ejecutar el programa de la camara con python vp-v2.py
- Luego se ejecuta el juego en linux
- Para fjar el tapete se te pediran 4 puntos, comienzas por la izquierda-inferior, luego derecha inferior, derecha superior y finalmente izquierda superior
- necesitas algo verde en el pie para que lo detecte

  
