# CommNext Redux

Port comunitario de [CommNext](https://github.com/Kerbalight/CommNext) para **Kerbal Space Program 2 Redux**.

## Estado actual

**v0.0.5 - nucleo CommNet + KSC + bandas/nodos**

- Carga como `KerbalMod` nativo de Redux.
- Hook real a `KSP.Game.CommNetManager` mediante Harmony.
- Estado CommNet, rango de antena, distancia de red y numero de nodos.
- Correccion del nodo origen sobre KSC y rango base de 2 Gm.
- Registro propio de nodos sincronizado con RegisterNode/UnregisterNode.
- Catalogo original de bandas: X, S, K, Ka y V.
- Los nodos vanilla se registran inicialmente en banda X con su rango real.
- UI oculta fuera de vuelo.

## Proyecto original

CommNext fue creado por **Kerbalight / leonardfactory**:
https://github.com/Kerbalight/CommNext

Este port conserva la licencia MIT y la atribucion original.

## Instalacion

Descarga el ZIP de la ultima version en **Releases** y copia `CommNextRedux` dentro de `<KSP2>/mods/`.

Las versiones 0.0.x son builds de desarrollo.
