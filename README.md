```
     _    ____  _  __   ____
    / \  |  _ \| |/ /  |  _ \ _ __ __ _  __ _ 'n' ____  _ __ ___  _ __
   / _ \ | |_) | ' /   | | | | '__/ _` |/ _` |   |  _ \| '__/ _ \| '_ \
  / ___ \|  __/| . \   | |_| | | | (_| | (_| |   | | | | | | (_) | |_) )
 /_/   \_\_|   |_|\_\  |____/|_|  \__,_|\__, |   |_| |_|_|  \___/| .__/
                                        |___/                   |_|
```

```
        .--------------------------------------.
        |  ______   _        __ ____  _         |
        | |___  /  (_)      / /|  _ \| |        |
        |    / /   __  _ __/ / | |_) | | ___  _ |
        |   / /   / _ \| '_ \/  |  _ <| |/ _ \(_)
        |  / /__  (_) | |_) |   | |_) | | (_) |_ |
        | /_____|\___/| .__/    |____/|_|\___/(_)|
        |             | |                        |
        |             |_|   ZOU. C'est tout.     |
        '--------------------------------------'
```

# APK Drag'n'drop

<p align="center">
  <img src="docs/screenshot.png" alt="APK Drag'n'drop en vrai, sur un vrai bureau, avec un vrai appareil branché" width="380">
</p>

Un utilitaire Windows qui fait **une seule chose** : glisser des `.apk` sur une
fenêtre, cocher un ou plusieurs téléphones, cliquer sur *Envoyer*. Fin de
l'histoire.

Pas de compte à créer. Pas d'installeur qui pose 40 questions. Pas de
télémétrie planquée dans un CGU de 60 pages. Pas d'IA embarquée pour
"améliorer votre expérience de drag'n'drop". Pas de mise à jour automatique
qui redémarre votre PC à 3h du matin.

```
   ┌──────────────────────────────┐
   │  [ Appareils ▾ ]      ( ↻ )  │
   ├──────────────────────────────┤
   │                              │
   │      ⬇  drop.apk ici         │
   │      (ou clique, feignasse)  │
   │                              │
   ├──────────────────────────────┤
   │  ● app-debug.apk  → Pixel 8  │  Installé ✅
   ├──────────────────────────────┤
   │        [   ENVOYER   ]       │
   └──────────────────────────────┘
```

## Ce que ça fait

- Tu glisses un ou plusieurs `.apk` → ils vont dans une file
- Tu coches un ou plusieurs téléphones Android branchés
- Tu cliques sur Send → `adb install -r` tourne tout seul, en parallèle sur
  chaque appareil, dans l'ordre sur chacun (pas de bordel USB)
- Tu regardes la petite pastille verte/rouge te dire si ça a marché

## Ce que ça ne fait pas

- Ça ne te demande pas ton email
- Ça ne synchronise rien dans le cloud
- Ça n'ouvre pas de navigateur
- Ça ne "phone home"
- Ça ne t'inflige pas un splashscreen de 5 secondes avec un logo qui tourne

## adb ?

Il est **dans le dossier**, à côté de l'exe (`adb/adb.exe`, binaire officiel
Google platform-tools). Rien à installer, rien à ajouter au PATH. Si tu as
déjà adb ailleurs, il utilisera aussi celui du PATH en secours.

## Lancer le bousin

```
double-clic sur ApkDragNDrop.exe
                │
                ▼
         ┌─────────────┐
         │   ZOU.  🚀   │
         └─────────────┘
```

Icône dans la barre système, clic droit dessus pour les Paramètres
(lancement avec Windows, rester en tray ou fermer pour de vrai à la
fermeture de la fenêtre — toi seul décides).

## Stack

WPF / .NET, un peu de glassmorphism pour le style, zéro dépendance NuGet
exotique, zéro framework JS planqué dans un Electron de 200 Mo pour afficher
un bouton.

```
  ┌─────────────────────────────────────┐
  │  "It's not much, but it's honest    │
  │   drag'n'drop work."                │
  └─────────────────────────────────────┘
```
