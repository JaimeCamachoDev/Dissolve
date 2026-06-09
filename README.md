<header>

![Movie_001](https://github.com/user-attachments/assets/9f2297f2-0a73-427e-98ba-21dceaf2b2d7)

![417098749-ebba7ef2-c7e4-48d9-b83b-ddc9a0a45a64](https://github.com/user-attachments/assets/87f91e32-028b-4491-a45e-1d957b058ead)

# **Dissolve**

_**Colección de shaders de disolución. Ideal para transiciones de aparición y desaparición.**_

</header>

---

## 📖 Descripción

**Dissolve** es un proyecto de Unity que reúne una colección de shaders de
disolución creados con **Shader Graph** sobre el **Universal Render Pipeline
(URP)**. Estos shaders permiten crear efectos de aparición y desaparición de
objetos de forma progresiva, controlando el borde de la disolución, su color
emisivo y la textura de ruido que define el patrón del efecto.

Son ideales para:

- Transiciones de aparición / desaparición de personajes y props.
- Efectos de teletransporte, materialización y desmaterialización.
- Muertes y *spawns* de enemigos (por ejemplo, zombis).
- Efectos visuales estilizados (VFX) para juegos y cinemáticas.

---

## ✨ Características

- 🎨 **Múltiples variantes de shader** construidas con Shader Graph.
- 🌐 **Disolución en espacio local y global**, con control de posición y rotación.
- 🧊 **Disolución triplanar** (Object Space y World Space) para texturizado sin UVs.
- 💧 Variantes especializadas: agua, líneas, áreas, RMAE y *unlit* para planos.
- ⬡ **Efecto Hexagonal (Hex)** para disoluciones tipo *ghost* / avatar.
- 🔆 Borde emisivo configurable para resaltar el frente de la disolución.
- 🎬 Integración con **Timeline** y **Unity Recorder** para grabar las transiciones.

---

## 🛠️ Requisitos

- **Unity 6** (`6000.0.42f1`) o superior.
- **Universal Render Pipeline (URP)** `17.0.4`.
- Paquetes utilizados:
  - `com.unity.render-pipelines.universal` (URP)
  - `com.unity.timeline`
  - `com.unity.recorder`
  - `com.jaimecamacho.unityfolders`

---

## 🚀 Instalación

1. Clona este repositorio:
   ```bash
   git clone https://github.com/JaimeCamachoDev/Dissolve.git
   ```
2. Abre el proyecto con **Unity 6 (6000.0.42f1)** a través de Unity Hub.
3. Espera a que Unity importe los paquetes y compile los shaders.
4. Abre la escena principal: `Assets/3-Scenes/Main.unity`.

---

## 📂 Estructura del proyecto

```
Assets/
├── 1-Programming/        # Scripts
├── 2-Art/
│   ├── 1-3D/             # Modelos 3D (p. ej. Zombie básico)
│   ├── 2-VFX/
│   │   ├── Dissolve/     # Shaders y materiales de disolución
│   │   └── Hex/          # Efecto de disolución hexagonal
│   ├── 3-SFX/            # Sonidos
│   ├── 4-Directors/      # Timelines / cinemáticas
│   ├── 5-Skyboxes/
│   ├── 6-Videos/
│   ├── 7-SolidMats/
│   ├── 8-PostProcessing/
│   ├── 9-UI/
│   └── 10-Lighting/
├── 3-Scenes/             # Main.unity
├── 4-Presets/
└── 5-Settings/           # Configuración de URP
```

---

## 🎨 Shaders incluidos

| Shader | Descripción |
|--------|-------------|
| `Dissolve_General` | Disolución básica y configurable de propósito general. |
| `Dissolve_Area` | Disolución limitada a un área. |
| `Dissolve_PosLocal_RotGlobal` | Disolución con posición local y rotación global. |
| `Dissolve_PosGlobal_RotGlobal` | Disolución con posición y rotación globales. |
| `Triplanar_Object Dissolve` | Disolución triplanar en espacio de objeto. |
| `Triplanar_World Dissolve` | Disolución triplanar en espacio de mundo. |
| `DissolveLinea_SHADER` | Disolución basada en líneas. |
| `Agua_SHADER Dissolve` | Variante de disolución para agua. |
| `VZ_RMAE Dissolve` | Disolución con flujo de trabajo RMAE. |
| `Hex Avatar Ghost / Astro` | Efecto de disolución hexagonal estilo *ghost*. |

> Cada shader cuenta con su material correspondiente en
> `Assets/2-Art/2-VFX/Dissolve/Material/`.

---

## 💡 Uso

1. Selecciona un objeto en la escena.
2. Asigna uno de los materiales de disolución desde
   `Assets/2-Art/2-VFX/Dissolve/Material/`.
3. Ajusta los parámetros del material (cantidad de disolución, color del borde,
   textura de ruido, etc.).
4. Anima el parámetro de disolución mediante scripts, *Timeline* o el inspector
   para producir la transición.

---

## 📄 Licencia

Este proyecto está disponible bajo la licencia **MIT**. Consulta el archivo
[LICENSE](LICENSE) para más detalles.

Copyright (c) 2025 Jaime Camacho

---

## 👤 Autor

**Jaime Camacho** — [JaimeCamachoDev](https://github.com/JaimeCamachoDev)

<footer>

</footer>
