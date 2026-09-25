# Hobo Rush

Hobo Rush is a 2D endless side-scrolling runner built in Unity. The player runs through a countryside road, jumps or slides past hazards, and tries to survive as long as possible while the score keeps increasing.

## Overview

This project was built in **Unity 6** as an endless runner focused on:
- distance-based score progression
- dynamic obstacle spawning
- increasing difficulty over time
- object pooling for stable long runs
- ScriptableObject-driven obstacle and difficulty data
- custom menu, pause, options, credits, and end-run UI
- music and sound effects for gameplay feedback

## Core Gameplay Loop

1. Start from the main menu.
2. Press Play to begin the endless run.
3. Jump or slide to avoid spikes, crates, and bats.
4. Build score as distance increases.
5. Speed and obstacle pressure increase over time.
6. Hit an obstacle to end the run, then restart or return to the lobby.

## Main Features

- **Endless runner gameplay** with no fixed ending
- **Score and best-score tracking**
- **Procedural obstacle spawning** using weighted obstacle data
- **Object pooling** for obstacle reuse and stable performance
- **ScriptableObjects** for difficulty and obstacle tuning
- **Pause menu** with restart, lobby, and exit controls
- **Options menu** for music, SFX, and best-score reset
- **Credits screen** listing the project team
- **Audio feedback** for jumping, sliding, warnings, milestones, impacts, UI clicks, and game over

## Controls

- **Space / Up Arrow**: Jump
- **S / Down Arrow**: Slide
- **Escape**: Pause menu
- **Mouse**: Menu and UI interaction

## Tech Stack

- **Engine:** Unity `6000.3.5f1`
- **Language:** C#
- **Rendering:** Universal Render Pipeline (URP)
- **Input:** Unity Input System

## Project Structure

- [`Assets/HoboRush`](Assets/HoboRush): main game content
- [`Assets/HoboRush/Scenes`](Assets/HoboRush/Scenes): main menu and gameplay scenes
- [`Assets/HoboRush/Scripts`](Assets/HoboRush/Scripts): gameplay, UI, audio, and spawning systems
- [`Assets/HoboRush/Data`](Assets/HoboRush/Data): ScriptableObject difficulty and obstacle data
- [`Assets/HoboRush/Prefabs`](Assets/HoboRush/Prefabs): pooled obstacle prefabs
- [`Docs`](Docs): project documentation
- [`Showcase`](Showcase): gameplay demo video

## Demo and Documentation

- **Project Documentation:** [Hobo-Rush-Documentation.docx](Docs/Hobo-Rush-Documentation.docx)
- **Gameplay Demo Video:** [Hobo-Rush-Video.mp4](Showcase/Hobo-Rush-Video.mp4)

## How to Open the Project

1. Open the project in **Unity 6000.3.5f1**.
2. Open [`MainMenu.unity`](Assets/HoboRush/Scenes/MainMenu.unity) to start from the full menu flow.
3. Open [`HoboRush.unity`](Assets/HoboRush/Scenes/HoboRush.unity) for direct gameplay testing.

## Team Credits

- Khalil Haidar
- Aseel Yassine
- Rawad Soufan

## Engineering Skills Demonstrated

- Built a reusable object-pooling system to avoid repeated runtime allocation during long play sessions
- Used ScriptableObjects to separate obstacle and difficulty tuning from gameplay code
- Coordinated player, spawning, scoring, audio, and UI states across the complete game loop
- Implemented persistent best-score and settings behavior between sessions
- Organized a production-style Unity project with focused folders for scripts, data, prefabs, scenes, and showcase assets

## Development Notes

A major challenge in development was balancing the endless loop so the game becomes harder without becoming unfair. Speed growth, obstacle spacing, bat height, jump timing, slide behavior, and score pacing were tuned together to keep the run readable and playable over time.
