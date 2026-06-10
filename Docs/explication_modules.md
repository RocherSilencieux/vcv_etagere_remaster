# 🎹 Guide de Fonctionnement des Modules (VCV Étagère Remaster)

Bienvenue dans le guide technique vulgarisé du projet ! Ici, on explique simplement comment marche chaque module sous le capot, avec les lignes de code clés à retenir pour ton jury.

---

## 1. VCO (Oscillateur Contrôlé en Tension) — `VcoModule.cs`
* **C'est quoi ?** Le cœur du synthétiseur. Il génère le son de base (la vibration) sous forme d'ondes mathématiques.
* **Comment ça marche ?** 
  * Il possède une variable `_phase` qui tourne en boucle entre $0$ et $2\pi$ (un cercle complet). À chaque échantillon de son traité, on ajoute un petit pas (`phaseIncrement`) qui dépend de la fréquence visée.
  * Il convertit cette phase en échantillon de tension selon la forme d'onde choisie.
* **Lignes clés à montrer :**
  * **La conversion 1V/Octave (Ligne ~69-70) :**
    ```csharp
    double currentFrequency = currentBaseFreq * Math.Pow(2.0, FrequencyInput.Value);
    ```
    *Vulgarisation :* Chaque volt entier ajouté double la fréquence (une octave plus haut). C'est pour ça qu'on utilise un calcul avec une puissance de 2.
  * **Le calcul des formes d'onde (Ligne ~218-240) :**
    * *Sinusoïdale :* `Math.Sin(_phase)` (oscillation fluide).
    * *Carrée (Square) :* `t < 0.5 ? 1.0f : -1.0f` (le son bascule brutalement de tout en haut à tout en bas).

---

## 2. VCF (Filtre Contrôlé en Tension) — `VcfModule.cs`
* **C'est quoi ?** Le sculpteur de timbre. Il permet de rendre le son plus sourd (couper les aigus) ou plus brillant (couper les graves).
* **Comment ça marche ?** 
  * Il implémente un filtre à variables d'état (SVF - State Variable Filter) d'après le modèle d'Andrew Simper.
  * Il calcule trois sorties simultanément : Passe-Bas (Lowpass), Passe-Haut (Highpass), et Passe-Bande (Bandpass).
* **Lignes clés à montrer :**
  * **L'algorithme de filtrage (Dans `Process`) :**
    Le code met à jour deux variables d'état (les mémoires du filtre, souvent appelées `_ic1eq` et `_ic2eq`) en faisant des combinaisons linéaires de l'entrée et des coefficients de coupure `g` et de résonance `R`.
    ```csharp
    float hp = (input - r * _ic1eq - _ic2eq) / (1f + g * (g + r));
    float bp = g * hp + _ic1eq;
    float lp = g * bp + _ic2eq;
    ```
    *Vulgarisation :* Le filtre calcule la différence entre le son actuel et ce qu'il a en mémoire pour adoucir ou accentuer les transitions rapides du signal.

---

## 3. ADSR (Générateur d'Enveloppe) — `AdsrModule.cs`
* **C'est quoi ?** Le contrôleur de dynamique. Il définit comment le volume d'une note évolue dans le temps (de l'attaque percutante au relâchement progressif).
* **Comment ça marche ?**
  * Il fonctionne comme une **machine à états** (State Machine) avec 5 étapes possibles : *Idle* (au repos), *Attack* (montée), *Decay* (redescente initiale), *Sustain* (maintien constant tant qu'on appuie), et *Release* (extinction progressive après relâchement).
* **Lignes clés à montrer :**
  * **La structure de l'automate (Dans `Process`) :**
    ```csharp
    switch (_state)
    {
        case EnvelopeState.Attack:
            _currentLevel += attackRate; ...
        case EnvelopeState.Decay:
            _currentLevel -= decayRate; ...
        case EnvelopeState.Release:
            _currentLevel -= releaseRate; ...
    }
    ```
    *Vulgarisation :* Le module surveille la tension de la porte (Gate). Quand le Gate passe à 1, il lance l'attaque. Quand le Gate retombe à 0, il saute directement à l'étape *Release*.

---

## 4. LFO (Oscillateur Basse Fréquence) — `LfoModule.cs`
* **C'est quoi ?** Le modulateur invisible. Il génère une onde très lente (souvent entre 0.1Hz et 20Hz), inaudible à l'oreille, mais parfaite pour faire osciller d'autres boutons automatiquement (comme le vibrato ou le wah-wah).
* **Comment ça marche ?**
  * C'est exactement le même principe qu'un VCO, mais calibré sur des fréquences minuscules.
* **Lignes clés à montrer :**
  * Sa sortie est souvent connectée à l'entrée FM d'un VCO ou d'un VCF pour faire bouger la hauteur ou la coupure du filtre de façon cyclique.

---

## 5. DELAY (Effet d'Écho) — `DelayModule.cs`
* **C'est quoi ?** Une machine à voyager dans le temps pour le son. Elle répète le signal avec un décalage temporel.
* **Comment ça marche ?**
  * Il utilise un **Buffer Circulaire** (un tableau de taille fixe où le pointeur d'écriture revient au début quand il atteint la fin, comme un serpent qui se mord la queue).
  * Pour éviter les petits cliquetis désagréables quand on change le temps d'écho en temps réel, il utilise une **interpolation linéaire**.
* **Lignes clés à montrer :**
  * **La lecture interpolée dans le passé :**
    ```csharp
    float sampleL = ReadBufferInterpolated(_bufferL, readPtrL);
    ```
    *Vulgarisation :* Si le temps d'écho demande à lire l'échantillon numéro 10.5, le code fait une moyenne pondérée entre l'échantillon 10 et l'échantillon 11 pour que la transition soit parfaitement lisse.

---

## 6. REVERB (Effet de Salle) — `ReverbModule.cs`
* **C'est quoi ?** Il simule l'acoustique d'une pièce ou d'une cathédrale en mélangeant des milliers d'échos très rapprochés.
* **Comment ça marche ?**
  * Il implémente une structure de réverbération classique (type Freeverb/Schroeder) combinant plusieurs **filtres en peigne** (Comb Filters) en parallèle, suivis de **filtres passe-tout** (All-Pass Filters) en série.
* **Lignes clés à montrer :**
  * **La propagation dans les filtres :**
    Chaque filtre retient le son pendant un nombre d'échantillons premier (pour éviter les résonances métalliques harmoniques) et le réinjecte avec un léger gain de feedback.

---

## 7. MIDI (Convertisseur Clavier > CV) — `MidiModule.cs`
* **C'est quoi ?** Le traducteur. Il prend les notes jouées sur ton clavier d'ordinateur (touches A, Z, E...) ou piano MIDI, et les traduit en signaux électriques (tensions) compréhensibles par les modules analogiques virtuels.
* **Comment ça marche ?**
  * Il expose deux ports de sortie :
    1. **Pitch (CV) :** La hauteur de la note calculée en Volt/Octave (ex. le La 440Hz correspond à 0.0V, un octave au-dessus vaut 1.0V).
    2. **Gate :** Une tension binaire (1.0V quand une touche est enfoncée, 0.0V quand elle est relâchée).
* **Lignes clés à montrer :**
  * **Le calcul du pitch V/Oct (Dans `PlayNote`) :**
    ```csharp
    PitchOutput.Value = (midiNote - 60) / 12f; // Le C4 (note MIDI 60) sert de référence (0V)
    GateOutput.Value = 1.0f;
    ```
    *Vulgarisation :* Comme il y a 12 demi-tons dans une octave, diviser la distance par 12 donne exactement le ratio de 1 Volt par Octave requis par le VCO.

---

## 8. SCOPE (Visualiseur Temporel) — `ScopeModule.cs`
* **C'est quoi ?** L'oscilloscope. Il dessine la courbe du son en temps réel pour que l'utilisateur puisse "voir" l'onde (sinus, carré, etc.).
* **Comment ça marche ?**
  * Il stocke les derniers échantillons reçus dans un tableau tampon (`_sampleBuffer`).
  * La vue WPF lit périodiquement ce tableau pour dessiner une ligne brisée (`Polyline` ou géométrie personnalisée) sur l'écran.

---

## 9. AUDIO OUT (Sortie Audio Principale) — `AudioOutputModule.cs`
* **C'est quoi ?** La passerelle finale vers les haut-parleurs physiques de l'ordinateur.
* **Comment ça marche ?**
  * Il collecte le signal stéréo gauche/droite entrant, lui applique le volume général configuré par le curseur (Master Volume), puis l'envoie à la carte son via la bibliothèque **NAudio**.
* **Lignes clés à montrer :**
  * **Le lissage du volume (Ligne ~91) :**
    ```csharp
    double currentVol = _masterVolumeRamp.Next();
    ```
    *Vulgarisation :* Si tu baisses brusquement le volume, la rampe effectue une transition douce en quelques millisecondes au lieu de couper net, évitant ainsi un craquement audio désagréable dans les haut-parleurs.

---

## 10. VCA (Amplificateur Contrôlé en Tension) — `VcaModule.cs`
* **C'est quoi ?** Le contrôleur de volume commandé par tension. Il fait varier l'amplitude du son en fonction d'un potentiomètre et d'une entrée de contrôle (comme une enveloppe ADSR ou un LFO).
* **Comment ça marche ?**
  * Il reçoit un signal audio en entrée (`L IN` / `R IN`) et calcule son amplitude de sortie en le multipliant par un coefficient de gain.
  * Si un câble de contrôle est connecté à `CV IN`, le gain est multiplié par la tension reçue sur ce port. Sinon, seul le curseur manuel `GAIN` détermine l'amplitude.
* **Lignes clés à montrer :**
  * **La modulation d'amplitude (Dans `Process`) :**
    ```csharp
    if (CvInput.IsConnected)
    {
        currentGain = CvInput.Value * _baseGain;
    }
    outputL = inputL * currentGain;
    ```
    *Vulgarisation :* C'est un multiplicateur électronique. Le son qui traverse le VCA est multiplié en temps réel par la valeur de contrôle. Par exemple, si l'enveloppe ADSR lui envoie `0.5`, le volume sonore sera réduit de moitié.
