# CommNext Redux

Port comunitario de [CommNext](https://github.com/Kerbalight/CommNext) para **Kerbal Space Program 2 Redux**.

## Estado actual

**v0.0.2 - base de compatibilidad**

- Carga como `KerbalMod` nativo de Redux.
- Detecta la nave activa.
- Comprueba la disponibilidad de `CommNetManager`.
- La ventana de diagnostico solo aparece cuando existe una nave activa.
- No se muestra en el menu principal.
- Compilado contra KSP2 v0.2.2.0 + Redux / SpaceWarp2.

Todavia no estan portadas todas las funciones originales. El siguiente trabajo se centrara en red, reles, bandas, oclusion, consumo electrico, renderizado de enlaces, configuracion, guardado y UI.

## Proyecto original

CommNext fue creado por **Kerbalight / leonardfactory**:
https://github.com/Kerbalight/CommNext

Este port conserva la licencia MIT y la atribucion original.

## Instalacion

Descarga el ZIP de la ultima version en **Releases** y copia la carpeta `CommNextRedux` dentro de `<KSP2>/mods/`.

## Desarrollo

Define la variable de entorno `KSP2DIR` con la ruta de tu instalacion de KSP2 Redux y ejecuta `build.ps1`.

Las versiones 0.0.x son builds de desarrollo.