# CommNext Redux

Port comunitario de CommNext para Kerbal Space Program 2 Redux.

Upstream: https://github.com/Kerbalight/CommNext

## v0.0.9

- Carga nativa como KerbalMod.
- Hook a KSP.Game.CommNetManager.
- Ventana de diagnostico solo en vuelo, nunca en VAB/OAB.
- Distancia fisica directa al nodo KSC.
- Correccion del nodo origen KSC y rango base de 2 Gm.
- Registro de nodos CommNet.
- Bandas X / S / K / Ka / V.
- Modulos Relay y Modulator portados.
- Patches de rangos de antenas originales.
- Consumo electrico de relÃ©s.
- Calculador paralelo de rutas CommNext con bandas, relÃ©s y oclusion planetaria.

El calculador CommNext todavia funciona en paralelo al ConnectionGraph vanilla. La siguiente fase es integrar sus resultados en el grafo real de Redux y restaurar renderizado/UI original.

## Instalacion

Descarga la ultima release y copia la carpeta CommNextRedux dentro de <KSP2>/mods/.

## Licencia

Se conserva la licencia MIT y atribucion del proyecto original.