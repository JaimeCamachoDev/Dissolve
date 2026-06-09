<header>

![Movie_001](https://github.com/user-attachments/assets/9f2297f2-0a73-427e-98ba-21dceaf2b2d7)

![417098749-ebba7ef2-c7e4-48d9-b83b-ddc9a0a45a64](https://github.com/user-attachments/assets/87f91e32-028b-4491-a45e-1d957b058ead)

# **Dissolve**

_**Colección de shaders de disolución para Unity. Ideal para transiciones de aparición y desaparición.**_

</header>

---

## Descripción

**Dissolve** es una colección de shaders para Unity centrada en efectos de disolución procedural. Diseñada para crear transiciones fluidas y visualmente impactantes cuando los objetos aparecen o desaparecen en escena, esta librería ofrece múltiples variantes adaptables a distintos estilos artísticos y necesidades técnicas.

La mayoría de los shaders están construidos con **Shader Graph** sobre el **Universal Render Pipeline (URP)** de Unity, lo que facilita su personalización sin necesidad de escribir código HLSL manualmente. Los shaders hexagonales (Hex) están escritos directamente como shaders de código.

---

## Características

- **13 shaders** de disolución y efectos relacionados listos para usar
- Construidos con **Shader Graph** sobre **URP 17.0.4**
- Compatibles con **Unity 6 (6000.0 LTS)**
- Múltiples variantes: por área, por posición (local/global), triplanar, lineal, agua y hexagonal
- Incluye una escena de demostración (`Main.unity`) con todos los efectos
- Incluye un personaje de ejemplo (zombie con animaciones) para probar los efectos

---

## Shaders incluidos

### Disolución (Shader Graph)

| Shader | Descripción |
|--------|-------------|
| Dissolve General | Disolución clásica basada en textura de ruido |
| Dissolve Area | Disolución delimitada por un área en espacio de mundo |
| Dissolve PosGlobal RotGlobal | Disolución que irradia desde una posición y rotación en espacio global |
| Dissolve PosLocal RotGlobal | Disolución desde una posición en espacio local con rotación global |
| Triplanar World Dissolve | Disolución triplanar en espacio de mundo |
| Triplanar Object Dissolve | Disolución triplanar en espacio de objeto |
| Triplanar Object Dissolve (Simply) | Versión simplificada de la disolución triplanar de objeto |
| Water Dissolve (Agua) | Disolución con efecto de agua/fluido |
| Line Dissolve (Linea) | Disolución lineal progresiva |
| VZ RMAE Dissolve | Disolución con soporte de mapa RMAE (Roughness, Metallic, AO, Emission) |

### Hexagonales (código)

| Shader | Descripción |
|--------|-------------|
| Hex Avatar Astro | Shader hexagonal estilo avatar astronauta |
| Hex Avatar Ghost | Shader hexagonal estilo avatar fantasma |
| Hex Specular | Shader hexagonal con especular |

---

## Requisitos

- **Unity 6 (6000.0 LTS)** o superior
- **Universal Render Pipeline (URP) 17.0.4** o superior
- Shader Graph habilitado en el proyecto

---

## Instalación

1. Clona o descarga este repositorio.
2. Ábrelo con **Unity 6 (6000.0 LTS)**.
3. Asegúrate de tener el paquete **URP** instalado (ya viene incluido en el proyecto).
4. Abre la escena `Assets/3-Scenes/Main.unity` para ver todos los efectos en acción.
5. Arrastra los materiales o shaders desde `Assets/2-Art/2-VFX/Dissolve/` (o `Assets/2-Art/2-VFX/Hex/` para los hexagonales) a tus propios objetos.

---

## Estructura del proyecto

```
Assets/
├── 1-Programming/          Scripts del proyecto
├── 2-Art/
│   ├── 1-3D/               Modelos y animaciones (incluye zombie de ejemplo)
│   └── 2-VFX/
│       ├── Dissolve/       Shaders y materiales de disolución
│       └── Hex/            Shaders, mallas y prefabs hexagonales
├── 3-Scenes/               Escena de demostración (Main.unity)
├── 4-Presets/
└── 5-Settings/             Configuración de URP
```

---

## Licencia

Consulta el archivo [LICENSE](LICENSE) para más información.

<footer>

</footer>
