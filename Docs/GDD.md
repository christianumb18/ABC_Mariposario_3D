# Game Design Document (GDD)

# Mariposario – Ciclo de Vida Lepidoptera

**Versión:** 1.0  
**Estado:** Documento de diseño funcional para desarrollo  
**Plataforma objetivo:** Android  
**Motor de desarrollo:** Unity  
**Género:** Videojuego educativo 3D de exploración, supervivencia ligera y progresión  
**Público objetivo:** Niños y jóvenes entre 8 y 18 años  
**Documento relacionado:** SRS v2.1 del proyecto Mariposario Virtual Interactivo + Ciclo de Vida Lepidoptera

\---

# 1\. Propósito del documento

El presente Game Design Document define el diseño integral del videojuego **Mariposario – Ciclo de Vida Lepidoptera**, con el fin de servir como guía para el equipo de desarrollo, diseño, modelado, implementación de gameplay, UI y validación educativa.

Este documento describe la experiencia del jugador, las mecánicas de juego, el sistema de progresión, el modelo de interacción, la estructura de pantallas, el enfoque visual, la lógica educativa y las decisiones principales de diseño. Su propósito es traducir los requerimientos generales del proyecto en un diseño jugable claro, coherente y ejecutable por el equipo técnico.

A diferencia del SRS, que define requerimientos funcionales y restricciones del sistema, este documento se concentra en **cómo se juega**, **qué siente el jugador**, **cómo progresa**, **qué aprende** y **cómo deben comportarse los sistemas del videojuego**.

\---

# 2\. Visión general del juego

## 2.1. Concepto general

**Mariposario – Ciclo de Vida Lepidoptera** es un videojuego educativo en 3D para dispositivos Android, desarrollado en Unity, en el que el jugador controla directamente diferentes especies de mariposas y vive su ciclo de vida de forma interactiva, desde la etapa de huevo hasta la adultez.

El juego combina exploración en tercera persona, mecánicas educativas, minijuegos táctiles, supervivencia ligera y progresión por especies. El objetivo principal no es únicamente entretener, sino también enseñar de forma activa el ciclo biológico de las mariposas, su relación con las plantas hospederas, sus amenazas naturales y su importancia ecológica.

El diseño parte de una experiencia visual low poly mundo abierto, colorida y accesible, pensada para públicos jóvenes, con una interfaz comprensible y mecánicas simples de aprender pero suficientemente variadas para mantener el interés del jugador.

\---

## 2.2. Fantasía del jugador

La fantasía central del juego es:

**“Vivir el mundo desde la perspectiva de una mariposa y experimentar su ciclo de vida completo dentro de un entorno natural interactivo.”**

El jugador no observa el ciclo de vida desde afuera, sino que lo recorre activamente. Esta decisión convierte el aprendizaje en experiencia directa y refuerza el valor pedagógico del videojuego.

\---

## 2.3. Objetivo del jugador

El jugador deberá:

* sobrevivir a las distintas etapas del ciclo de vida, depredadores y posarse en una planta hospedera que no corresponde puede restar puntos;
* completar acciones biológicas propias de cada fase;
* aprender sobre especies, plantas hospederas y procesos ecológicos;
* acumular puntos, experiencia como xp y tiempo de vuelo;
* desbloquear nuevas especies de mariposas;
* completar progresivamente la enciclopedia del juego (solamente de las especies mencionadas con la Ingenieria).

\---

## 2.4. Propuesta de valor

La propuesta de valor del videojuego se basa en cuatro pilares:

1. **Aprendizaje activo:** el usuario aprende jugando, no solo leyendo.
2. **Cambio de perspectiva:** el jugador encarna a la mariposa y luego a la larva.
3. **Progresión significativa:** cada especie desbloqueada representa avance educativo.
4. **Apropiación del conocimiento ecológico:** el contenido se integra a la jugabilidad, no aparece como elemento aislado.

\---

# 3\. Público objetivo

El videojuego está dirigido principalmente a:

* niños;
* adolescentes;
* jóvenes;
* instituciones educativas;
* escenarios de divulgación ambiental.

La franja principal de diseño corresponde a usuarios entre **8 y 18 años según el documento SRS**, por lo que las decisiones de interfaz, lenguaje, dificultad y presentación del contenido deben mantener equilibrio entre claridad, atractivo visual y profundidad educativa.

\---

# 4\. Plataforma y restricciones de diseño

## 4.1. Plataforma principal

El videojuego será diseñado para **Android**, priorizando controles táctiles y sesiones de juego relativamente cortas.

## 4.2. Implicaciones de diseño para Android

Al ser un juego móvil, el diseño debe priorizar:

* lectura rápida;
* controles táctiles intuitivos;
* HUD visible pero no invasivo;
* interacción por gestos simples;
* sesiones breves o interrumpibles;
* guardado automático;
* rendimiento estable en dispositivos móviles.

## 4.3. Restricciones asumidas

* El juego debe ser legible en pantallas para la mayoría de dispositivos móviles.
* Las acciones principales deben poder ejecutarse con una o dos manos.
* El texto no debe saturar la pantalla.
* La cámara no debe dificultar la orientación del jugador.
* La complejidad mecánica debe aumentar por etapas, no desde el inicio.

\---

# 5\. Género y estructura general de la experiencia

El juego combina elementos de varios subgéneros:

* exploración educativa;
* aventura 3D en tercera persona;
* simulación biológica simplificada;
* supervivencia ligera;
* colección y progresión;
* minijuegos táctiles contextualizados.

No se plantea como un juego competitivo ni como un simulador hiperrealista. El diseño busca una experiencia accesible, fluida y pedagógica, donde la presión del fracaso existe, pero sin castigar de forma excesiva al usuario.

\---

# 6\. Bucle principal de juego

El bucle principal del juego es:

**Explorar → interactuar → aprender → ganar puntos/XP → sobrevivir → evolucionar → desbloquear contenido**

Este bucle se adapta a cada etapa del ciclo de vida, pero mantiene una lógica común:

1. el jugador detecta un objetivo biológico o interactivo;
2. realiza una acción propia de la etapa actual;
3. recibe retroalimentación visual, educativa y de progresión;
4. acumula puntos, experiencia u horas de vuelo;
5. avanza hacia la siguiente fase o desbloquea nuevo contenido.

\---

# 7\. Estructura general del juego

## 7.1. Pantallas principales

El flujo principal de pantallas es el siguiente:

1. Pantalla de inicio
2. Selección de Bioma y luego de especie
3. Ficha informativa de la especie solo si el usario hace touche screen sobre la especie.
4. Inicio de experiencia
5. Gameplay de ciclo de vida mundo abierto
6. Eventos educativos contextuales
7. Resultado parcial / progreso
8. Desbloqueo / enciclopedia
9. Continuación o salida

## 7.2. Inicio

La pantalla inicial presenta el nombre del juego, acceso al modo principal, enciclopedia y demás opciones básicas del sistema como configuración y creación del perfil para guardar su sesión.

## 7.3. Selección de especie

El jugador accede a una galería de mariposas. Algunas estarán disponibles y otras bloqueadas. Cada especie seleccionada mostrará una ficha breve si el usuario lo desea.

## 7.4. Inicio de juego

Tras seleccionar la especie, el jugador entra al mapa del bioma previamente seleccionado y puede ir explorando, va logrando tarea, misiones o etapas una tras otra.

\---

# 8\. Cámara y perspectiva

## 8.1. Perspectiva general

La cámara principal del juego será en **tercera persona**.

## 8.2. Justificación

La tercera persona permite:

* apreciar mejor el modelo 3D de la mariposa;
* reforzar el valor visual y educativo de la especie;
* facilitar orientación espacial;
* mostrar mejor animaciones de vuelo, alimentación y metamorfosis;
* cambiar naturalmente a control de larva en etapas terrestres.

## 8.3. Comportamiento de cámara

La cámara deberá:

* seguir al personaje principal de forma suave;
* mantener buena visibilidad del entorno;
* evitar colisiones bruscas;
* adaptarse a espacios abiertos y cerrados;
* reforzar la escala del personaje frente al escenario natural.

En la fase de mariposa la cámara se ubicará por detrás y ligeramente arriba del personaje. En la fase de larva se acercará más al suelo para transmitir vulnerabilidad y cambio de escala.

\---

# 9\. Estructura jugable por etapas del ciclo de vida

El juego se organiza en cuatro etapas principales dentro de la parte interactiva-educativa como misiones principales:

1. Huevo
2. Larva
3. Crisálida
4. Mariposa adulta

Cada etapa modifica la jugabilidad y representa un comportamiento biológico diferente.

\---

## 9.1. Etapa 1: Huevo

### Descripción

El jugador inicia en la fase de huevo. Esta etapa representa el nacimiento y la salida del cascarón.

### Objetivo jugable

Eclosionar con éxito y pasar a la etapa de larva.

### Mecánica principal

Minijuego táctil de interacción repetida, **el gesto se simplifca de manera visual en el figma**. 

### Ejemplo de interacción

* tocar varias veces;
* deslizar repetidamente;
* ampliar o ejercer gesto de ruptura;
* completar una pequeña barra de progreso de eclosión.

### Propósito educativo

Mostrar que el nacimiento de la mariposa es un proceso delicado y que el huevo constituye la primera etapa del ciclo, si lo ahce muy rapido o muy lento puede perder y tendra que iniciar de nuevo.

### Resultado

Al completar la acción, se activa la transición a larva.

\---

## 9.2. Etapa 2: Larva

### Descripción

El jugador controla ahora a la larva en tercera persona desde el suelo.

### Cambio de jugabilidad

Esta es una de las transformaciones más importantes del diseño: el jugador deja de volar y pasa a desplazarse lentamente por el entorno, buscando alimento y evitando amenazas.

### Objetivos jugables

* desplazarse por el entorno terrestre;
* localizar la planta hospedera adecuada;
* alimentarse;
* crecer;
* sobrevivir a depredadores;
* alcanzar el punto de madurez necesario para crear la crisálida.

### Mecánicas principales

* movimiento terrestre en tercera persona;
* alimentación interactiva;
* barra de crecimiento;
* gestión de vida;
* detección de depredadores;
* progresión hacia metamorfosis.

### Riesgo y supervivencia

La larva es vulnerable. Podrá recibir daño por ataques o proximidad de depredadores.

### Condición de derrota

Si la vida de la larva llega a cero, la etapa se reinicia desde **el último punto de control.**

### Propósito educativo

La fase larvaria enseña que la alimentación y la supervivencia son esenciales para el desarrollo de la especie.

\---

## 9.3. Etapa 3: Crisálida

### Descripción

La larva, una vez alimentada y suficientemente desarrollada, puede acercarse a la planta o punto adecuado para iniciar la etapa de crisálida.

### Objetivos jugables

* alcanzar el estado de madurez;
* posicionarse en el lugar correcto;
* activar el proceso de transformación;
* completar la interacción de metamorfosis.

### Mecánicas principales

* transición contextual;
* minijuego táctil o secuencia breve;
* espera activa o evento interactivo;
* representación visual de la crisálida.

### Propósito educativo

Enseñar que la metamorfosis no es instantánea, sino un cambio biológico crucial del ciclo.

\---

## 9.4. Etapa 4: Mariposa adulta

### Descripción

La mariposa emerge y el jugador recupera movilidad aérea en tercera persona.

### Objetivos jugables

* volar libremente;
* explorar;
* posarse;
* alimentarse del néctar;
* mantener energía y resistencia;
* poner huevos en la planta adecuada;
* cerrar el ciclo biológico.

### Mecánicas principales

* vuelo libre;
* planeo y desplazamiento;
* posado en flores;
* alimentación;
* búsqueda de planta hospedera;
* postura de huevos;
* eventos de quiz contextuales.

### Propósito educativo

Conectar la adultez con reproducción, polinización, relación planta-especie y continuidad del ciclo.

\---

# 10\. Mecánicas principales del juego

## 10.1. Movimiento

### Mariposa

* desplazamiento libre en el aire;
* maniobra direccional;
* ascenso y descenso en este caso el jugador debe apoyarse de la camara, su mano izquierda moverá el jostick para indicar dirección, su mano derecha movera la camara con el pulgar, aqui se producria en ascenso o descenso dependiendo del moviento del usurio.
* posado contextual.

### Larva

* desplazamiento terrestre;
* orientación por superficie;
* velocidad reducida;
* movilidad vulnerable.

\---

## 10.2. Interacción contextual

El juego usa acciones contextuales cercanas al entorno. Cuando el jugador se aproxima a un elemento relevante, aparecerán acciones disponibles.

Ejemplos:

* comer;
* posar;
* poner huevo;
* eclosionar;
* interactuar con contenido educativo;
* activar transición de etapa.

\---

## 10.3. Alimentación

La alimentación funciona como sistema jugable y como vehículo educativo.

### En fase mariposa

La mariposa se alimenta del néctar de flores compatibles.

### En fase larva

La larva consume hojas de su planta hospedera.

### Resultado

La alimentación recupera o incrementa variables como:

* vida;
* energía;
* progreso de crecimiento;
* capacidad de avanzar a la siguiente etapa.

\---

## 10.4. Sistema de vida

La vida representa la capacidad de supervivencia del personaje actual.

### Afectada por:

* ataques de depredadores;
* falta de recursos;
* errores en determinadas acciones.

### Recuperación:

* alimentación;
* acciones correctas;
* avance controlado en la etapa.

\---

## 10.5. Sistema de energía y estamina

### Vida

Representa la disponibilidad biológica general del personaje para seguir activo o con vida.

### Estamina

Representa la capacidad inmediata de esfuerzo, especialmente en vuelo o movimiento continuo, el vuelo implica un desgaste de esta, el comer la recupera. (si se queda sin estamina, ira perdiendo vida progresivamente).

### Implicación jugable

* volar o moverse constantemente consume estamina;
* alimentarse ayuda a restaurarla;
* si la estamina disminuye demasiado, el jugador deberá posarse o recuperarse.

\---

## 10.6. Depredadores

Los depredadores son amenazas del entorno que introducen tensión y aprendizaje ecológico. (los depredadores se irán proponiendo y modelando en el trayecto del desarrollo, asi como su logica de non playable character)

### En etapa larva

Su presencia es crítica. Esta fase concentra la mayor vulnerabilidad del jugador.

### Funciones del sistema

* reducir vida;
* obligar al desplazamiento cauteloso;
* reforzar la idea de supervivencia natural;
* evitar que la experiencia se limite a acciones pasivas.

### Comportamiento esperado

* patrullaje o presencia cercana;
* activación de amenaza por proximidad;
* daño al contacto o persecución breve.

\---

## 10.7. Reinicio por muerte

Si el personaje muere, el juego reinicia desde el último punto de control válido de la etapa actual. El diseño evita reinicios excesivamente largos para no frustrar al jugador joven.

\---

# 11\. Sistema de progresión

## 11.1. Filosofía de progresión

La progresión debe recompensar tanto el aprendizaje como la exploración y la habilidad básica del jugador. No se busca una experiencia punitiva, sino un avance que motive a completar especies y descubrir contenido nuevo.

## 11.2. Variables de progresión

El juego manejará tres formas de progreso:

* **Puntos**
* **Experiencia (XP)**
* **Horas de vuelo**

### Puntos

Se obtienen por acciones correctas, respuestas acertadas e interacciones valiosas.

### XP

Representa progreso general del jugador.

### Horas de vuelo

Funcionan como una métrica temática del tiempo y dominio acumulado en la experiencia aérea.

\---

## 11.3. Acciones que otorgan recompensa

Ejemplo base de sistema:

* descubrir especie: +10 puntos
* leer o activar ficha educativa: +5 puntos
* responder bien un mini quiz: +20 puntos
* completar una etapa: +30 puntos
* completar ciclo de vida completo: +50 puntos
* polinizar o alimentarse correctamente: +10 puntos
* explorar o sostener tiempo efectivo de vuelo: otorga XP/horas de vuelo
* La XP puede permitir subir del nivel al usuario, a mayor nivel puede ir mejorar la mariposa en velocidad, defensa o incluso ataque
* *El tiempo de vuelo (esta pendiente o por definirse, queda a criterio del equipo)*

## 11.4. Uso del progreso

El progreso permitirá:

* desbloquear nuevas especies;
* ampliar la enciclopedia;
* mejorar atributos del personaje;
* extender la duración y resistencia de vuelo;
* aumentar vida o resistencia en especies futuras.

\---

## 11.5. Desbloqueo de especies

El desbloqueo será progresivo.

### Regla general

El jugador inicia con una especie disponible. Al completar ciclos, responder correctamente eventos educativos y alcanzar umbrales de puntos/XP, desbloquea nuevas mariposas.

### Catálogo

El catálogo final será escalable. La base de diseño contempla un conjunto amplio de especies, con parte del inventario heredado y expansión posterior. El número final exacto podrá ajustarse tras validación del contenido biológico y disponibilidad de assets.

\---

# 12\. Enciclopedia

## 12.1. Función

La enciclopedia es uno de los principales sistemas de recompensa y consolidación del aprendizaje.

## 12.2. Propósito

* registrar especies desbloqueadas;
* mostrar conocimiento acumulado;
* reforzar el componente educativo;
* motivar la colección y finalización del juego.

## 12.3. Contenido por entrada

Cada entrada de enciclopedia podrá incluir:

* nombre común;
* nombre científico;
* imagen o modelo representativo;
* etapa del ciclo;
* planta hospedera;
* alimentación;
* región o hábitat;
* curiosidades;
* amenazas naturales;
* observaciones del ciclo de vida.

## 12.4. Enciclopedia de larvas

También se contempla contenido específico para la fase larvaria, dada su relevancia jugable y educativa dentro del proyecto.

\---

# 13\. Sistema educativo

## 13.1. Enfoque pedagógico

El contenido educativo no debe sentirse separado del juego. Debe aparecer integrado a las acciones del jugador.

## 13.2. Modelo de integración

Los eventos educativos aparecerán de forma contextual, automática o semiautomática, asociados a acciones concretas del gameplay.

Ejemplos:

* al alimentarse;
* al poner huevo;
* al eclosionar;
* al descubrir una planta hospedera;
* al completar la crisálida;
* al cerrar el ciclo.

## 13.3. Mini quizzes

Cada evento educativo podrá activar un mini quiz de **1 o 2 preguntas como máximo**.

### Justificación

Este límite mantiene el ritmo del juego y evita romper la experiencia por exceso de lectura.

## 13.4. Recompensa educativa

Responder correctamente:

* otorga puntos;
* aporta XP;
* contribuye al desbloqueo de especies;
* refuerza el progreso de enciclopedia.

Responder incorrectamente:

* no debe castigar severamente;
* puede mostrar la respuesta correcta con una explicación breve;
* mantiene el foco pedagógico por encima del castigo.

## 13.5. Tipo de preguntas

* selección múltiple;
* verdadero/falso;
* relación simple entre especie y planta;
* identificación de etapa;
* pregunta breve contextual.

\---

# 14\. Diseño del HUD

## 14.1. Principios generales

El HUD debe ser claro, visible y ligero. Debe aportar información útil sin saturar la pantalla.

## 14.2. Elementos del HUD

### Siempre visibles o prioritarios

* vida;
* energía;
* estamina;
* puntos;
* indicador de experiencia o progreso;
* joystick virtual;
* indicador de dirección u orientación;
* botones o prompts de interacción contextual.

### Contextuales

* “comer”;
* “poner huevo”;
* “posarse”;
* “eclosionar”;
* “mirar video” o “ver contenido educativo”;
* transición de etapa.

## 14.3. Horas de vuelo

Las horas de vuelo funcionan como métrica temática y pueden mostrarse:

* en menús de progreso;
* en resultados parciales;
* en paneles de especie;
* no necesariamente de forma invasiva en todo momento.

\---

# 15\. Controles

## 15.1. Controles base para Android

* joystick virtual izquierdo para movimiento;
* gesto táctil o arrastre para orientar acciones según diseño final;
* botones contextuales de acción;
* taps repetidos o swipes para minijuegos;
* controles simplificados según etapa.

## 15.2. Filosofía de control

El diseño de control debe ser:

* intuitivo;
* consistente;
* apto para público infantil y juvenil;
* adaptado a sesiones rápidas;
* coherente con cada transformación del personaje.

## 15.3. Variación de controles por etapa

### Huevo

gestos simples de ruptura o eclosión.

### Larva

movimiento terrestre y acción contextual de comer.

### Crisálida

interacciones limitadas y guiadas.

### Mariposa adulta

vuelo, posado, alimentación, exploración y reproducción.

\---

# 16\. Diseño del mundo y nivel

## 16.1. Tipo de entorno

El juego se plantea como un entorno abierto controlado, con sensación de pequeño mundo abierto educativo.

No se busca un mapa masivo, sino un espacio natural amplio, explorable, reconocible y funcional para móviles.

## 16.2. Componentes del entorno

* montañas low poly;
* árboles y vegetación estilizada;
* flores grandes e identificables;
* plantas hospederas;
* zonas de vuelo;
* zonas terrestres para larva;
* áreas de transición;
* puntos de interés ecológico.

## 16.3. Diseño ambiental

El mundo debe comunicar:

* naturaleza;
* color;
* vida;
* escala biológica;
* accesibilidad visual.

\---

# 17\. Dirección de arte y estilo visual

## 17.1. Estilo artístico

El videojuego tendrá un estilo **low poly educativo**, con formas geométricas simples, colores vivos y composición clara.

## 17.2. Objetivos del estilo

* facilitar el rendimiento en Android;
* favorecer legibilidad visual;
* transmitir una identidad amigable;
* resaltar a las mariposas y plantas;
* evitar ruido visual innecesario.

## 17.3. Referencias visuales internas del proyecto

La interfaz y mockups muestran:

* paisajes de montaña y bosque;
* vegetación simplificada;
* flores de gran tamaño;
* mariposas como foco visual principal;
* HUD con colores cálidos y acentos celestes.

## 17.4. Paleta visual base

La paleta se organiza alrededor de:

* verdes naturales para vegetación y suelo;
* naranjas y amarillos para mariposas, flores y elementos de recompensa;
* celestes o turquesas para indicadores interactivos;
* blancos o tonos claros para textos y paneles;
* tonos tierra suaves para bases, rocas y elementos del entorno.

## 17.5. UI visual

La interfaz debe mantener:

* contraste suficiente;
* botones legibles;
* tipografía clara;
* paneles informativos limpios;
* jerarquía visual inmediata.

\---

# 18\. Audio y retroalimentación

## 18.1. Objetivo del audio

El sonido debe reforzar:

* calma natural;
* inmersión ambiental;
* recompensa por interacción;
* claridad de eventos.

## 18.2. Elementos sonoros sugeridos

* ambiente natural;
* aleteo;
* sonido leve al alimentarse;
* sonido de acierto en quizzes;
* retroalimentación de desbloqueo;
* señal de peligro por depredador;
* transición de metamorfosis.

## 18.3. Feedback visual

Toda acción importante debe tener feedback visual:

* barras;
* brillos;
* partículas;
* animaciones;
* iconos contextuales;
* cambios de estado evidentes.

\---

# 19\. Sistema de guardado

## 19.1. Estrategia recomendada

Se recomienda **guardado automático**, o tambien manual habilitado en el menú.

## 19.2. Razones de diseño

El guardado automático:

* simplifica la experiencia en móvil;
* evita pérdida accidental de progreso;
* reduce fricción para públicos jóvenes;
* encaja mejor con sesiones cortas.

## 19.3. Momentos de guardado

El juego guardará automáticamente cuando:

* se complete una etapa;
* se desbloquee una especie;
* se registre nueva entrada en enciclopedia;
* se complete un quiz relevante;
* se alcance un punto biológico importante, como planta hospedera o transición de fase.

## 19.4. Checkpoints

Además del guardado general, cada etapa puede manejar puntos de control internos para reducir frustración en caso de derrota.

\---

# 20\. Dificultad

## 20.1. Filosofía

La dificultad debe ser progresiva, justa y comprensible.

## 20.2. Escalada

* primeras especies: dificultad baja;
* especies posteriores: mayor exigencia en supervivencia, exploración y conocimiento;
* quizzes: breves, nunca excesivos;
* depredadores: amenaza real pero legible.

## 20.3. Fracaso

El fracaso debe enseñar y permitir reintento rápido. El castigo excesivo contradice el propósito pedagógico del juego.

\---

# 21\. Estructura de sesión de juego

## 21.1. Duración objetivo

La sesión puede plantearse en modos aproximados de:

* 5 minutos;
* 10 minutos;
* 15 minutos.

Esto facilita uso en:

* clase;
* práctica individual;
* exhibiciones;
* contextos de divulgación educativa.

## 21.2. Fragmentación natural

Cada etapa del ciclo funciona como segmento natural de sesión.

\---

# 22\. Diseño técnico orientado a implementación

## 22.1. Sistemas principales a desarrollar

* controlador de mariposa en tercera persona;
* controlador de larva en tercera persona;
* sistema de estados por etapa biológica;
* sistema de interacción contextual;
* sistema de puntos y XP;
* sistema de horas de vuelo (opcional);
* gestor de quizzes contextuales;
* sistema de enciclopedia;
* sistema de desbloqueo de especies;
* HUD adaptable;
* checkpoints y guardado automático y manual;
* IA o lógica de depredadores (npc´s);
* administrador de transición entre fases.

## 22.2. Organización lógica recomendada

Se recomienda organizar el proyecto en módulos:

* `Core`
* `Player`
* `Lifecycle`
* `UI`
* `Education`
* `Progression`
* `Encyclopedia`
* `SaveSystem`
* `Enemies`
* `SpeciesData`

## 22.3. Datos escalables

La información de especies, quizzes y contenido enciclopédico debe almacenarse en estructuras escalables, de preferencia externas o desacopladas de la lógica principal, para permitir crecimiento del catálogo sin reescribir sistemas base.

\---

# 23\. Riesgos de diseño

## 23.1. Riesgo de sobrecarga de mecánicas

Si cada etapa agrega demasiadas acciones, el juego puede perder claridad.

## 23.2. Riesgo de saturación educativa

Demasiado texto o demasiadas preguntas romperían el ritmo jugable.

## 23.3. Riesgo de complejidad técnica móvil

El vuelo, la cámara y el rendimiento deben cuidarse especialmente en Android.

## 23.4. Riesgo de inconsistencia entre especies

El catálogo deberá definirse con criterio para no prometer más contenido del que el equipo puede producir con calidad.

\---

# 24\. Criterios de éxito del diseño

El diseño se considerará exitoso si logra que:

* el jugador entienda el ciclo de vida jugando;
* la transición entre etapas sea clara y atractiva;
* el control de mariposa y larva se sienta diferente pero coherente;
* el sistema educativo no rompa el ritmo;
* la progresión motive a seguir jugando;
* la enciclopedia funcione como recompensa real;
* el juego sea legible, estable y disfrutable en Android.

\---

# 25\. Conclusión de diseño

**Mariposario – Ciclo de Vida Lepidoptera** está concebido como una experiencia educativa interactiva en la que la jugabilidad y el contenido biológico avanzan juntos. El jugador no solo observa el ciclo de vida de una mariposa, sino que lo encarna, lo recorre, lo sobrevive y lo comprende.

La decisión de diseñar el juego en tercera persona, con control directo de mariposa y larva, progresión por especies, quizzes contextuales breves, sistema de puntos, XP, horas de vuelo y enciclopedia desbloqueable, permite construir una experiencia clara, escalable y pedagógicamente consistente.

Este documento establece la base de diseño para que el equipo de ingeniería, arte, UI, contenido y QA pueda implementar el videojuego con una visión común, coherente y técnicamente viable.

\---

# 26\. Anexo breve de decisiones cerradas de diseño

* Plataforma principal: Android
* Perspectiva principal: tercera persona
* Personaje jugable: mariposa y larva, según la etapa
* Estilo visual: low poly educativo
* Progresión: desbloqueo progresivo de especies
* Recompensas: puntos, XP y horas de vuelo(opcional)
* Sistema educativo: mini quizzes contextuales de 1 o 2 preguntas
* HUD principal: vida, energía, estamina, puntos e interacción contextual
* Enciclopedia: sí, desbloqueable y asociada al progreso
* Derrota: reinicio desde checkpoint
* Guardado: automático y manual 

