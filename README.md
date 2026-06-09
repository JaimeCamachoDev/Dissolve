<header>
   
![Movie_001](https://github.com/user-attachments/assets/9f2297f2-0a73-427e-98ba-21dceaf2b2d7)

![417098749-ebba7ef2-c7e4-48d9-b83b-ddc9a0a45a64](https://github.com/user-attachments/assets/87f91e32-028b-4491-a45e-1d957b058ead)




# **Dissolve**

_**Colección de shaders de disolución. Ideal para transiciones de aparición y desaparición.**_

</header>

---

## Descripción

**Dissolve** es una colección de shaders para Unity centrada en efectos de disolución procedural. Diseñada para crear transiciones fluidas y visualmente impactantes cuando los objetos aparecen o desaparecen en escena, esta librería ofrece múltiples variantes adaptables a distintos estilos artísticos y necesidades técnicas.

Todos los shaders están construidos con **Shader Graph** dentro del **Universal Render Pipeline (URP)** de Unity, lo que facilita su personalización sin necesidad de escribir código HLSL manualmente.

---

## Características

- **11 shaders de disolución** listos para usar en producción
- Construidos con **Shader Graph** (URP 17.x)
- Compatibles con **Unity 2023 LTS**
- Múltiples variantes: por área, por posición, triplanar y más
- Incluye una escena de demostración (`Main.unity`) con todos los efectos

---

## Shaders incluidos

| Shader | Descripción |
|--------|-------------|
| Dissolve General | Disolución clásica basada en textura de ruido |
| Dissolve Area | Disolución delimitada por área en espacio mundo |
| Dissolve Position | Disolución que irradia desde un punto de origen |
| Triplanar Dissolve (World) | Disolución triplanar en espacio de mundo |
| Triplanar Dissolve (Object) | Disolución triplanar en espacio de objeto |
| Water Dissolve | Disolución con efecto de agua/fluido |
| Line Dissolve | Disolución lineal progresiva |
| RMAE Dissolve | Disolución con soporte de mapa RMAE (Roughness, Metallic, AO, Emission) |
| Hex Avatar Astro | Shader hexagonal estilo avatar astronauta |
| Hex Avatar Ghost | Shader hexagonal estilo avatar fantasma |
| Hex Specular | Shader hexagonal con especular |

---

## Requisitos

- **Unity 2023 LTS** o superior
- **Universal Render Pipeline (URP) 17.0.4** o superior
- Shader Graph habilitado en el proyecto

---

## Instalación

1. Clona o descarga este repositorio.
2. Abre el proyecto con **Unity 2023 LTS**.
3. Asegúrate de tener el paquete **URP** instalado (incluido en el proyecto).
4. Abre la escena `Assets/3-Scenes/Main.unity` para ver todos los efectos en acción.
5. Arrastra los materiales o shaders desde `Assets/2-Art/2-VFX/Dissolve/Shader/` a tus propios objetos.

---

<footer>
   

</footer>
