# Crab Monster Integration Guide

This guide explains how to integrate the LB3D Crab Monster into your island scene with AI behaviors.

## Overview

The integration includes:
- **PlayerHealth System**: Manages player health and damage
- **CrabMonsterAI**: Advanced AI with wandering, chasing, and attacking behaviors
- **CrabMonsterSpawner**: Spawns multiple crab monsters in the scene
- **CrabMonsterSetup**: Helper script to configure crab monster prefabs

## Setup Instructions

### 1. Player Health Setup

1. **Add PlayerHealth component to your player**:
   - Select your player GameObject (the one with FirstPersonController)
   - Add Component ? Scripts ? PlayerHealth
   - Configure health settings (default: 100 HP)

2. **Optional UI Setup**:
   - Create UI Canvas with Health Bar (Slider) and Health Text
   - Assign these to the PlayerHealth component's UI fields

### 2. Crab Monster Prefab Setup

1. **Locate the Crab Monster Prefab**:
   - Find the crab-monster prefab in `Assets/LB3D/CrabMonster/`

2. **Add AI Components**:
   - Select the crab-monster prefab
   - Add Component ? Scripts ? CrabMonsterSetup
   - In Play mode or using the context menu, run "Setup Crab Monster"
   - This will automatically add NavMeshAgent, AudioSource, and CrabMonsterAI components

3. **Configure Animation Parameters**:
   - Make sure your Animator Controller has the following parameters:
     - `Walking_2` (Bool) - Slow walking animation for wandering
     - `Walking_1` (Bool) - Fast walking animation for chasing
     - `Idle` (Bool) - Idle animation
     - `Attack_1`, `Attack_2`, `Attack_3` (Triggers) - Attack animations

### 3. Scene Setup

1. **NavMesh Baking**:
   - Go to Window ? AI ? Navigation
   - Select your terrain/ground objects
   - Mark them as "Navigation Static"
   - Click "Bake" to generate NavMesh

2. **Spawn Crab Monsters**:
   - Create an empty GameObject in your scene
   - Add Component ? Scripts ? CrabMonsterSpawner
   - Assign your configured crab-monster prefab
   - Configure spawn settings (count, radius, etc.)

### 4. Advanced Configuration

#### CrabMonsterAI Settings:
- **Detection Range**: How far the crab can detect the player (default: 10m)
- **Chase Range**: How far the crab will chase before giving up (default: 15m)
- **Attack Range**: Distance at which crab will attack (default: 2.5m)
- **Wander Speed**: Speed during wandering (default: 2 m/s)
- **Chase Speed**: Speed during chasing (default: 6 m/s)
- **Attack Damage**: Damage per attack (default: 25 HP)

#### Animation Integration:
The AI system expects these animation states:
- **Walking_2**: Used for slow wandering movement
- **Walking_1**: Used for fast chase movement  
- **Idle**: Used when stationary
- **Attack_1/2/3**: Random attack animations

## Behavior Description

### Wandering State
- Crab moves slowly around the area using Walking_2 animation
- Randomly picks nearby waypoints within the wander radius
- Constantly checks for player within detection range

### Chasing State  
- Triggered when player enters detection range and is visible
- Crab switches to Walking_1 animation (faster movement)
- Follows player using NavMesh pathfinding
- Returns to wandering if player gets too far away

### Attacking State
- Triggered when player is within attack range
- Crab stops moving and faces the player
- Plays random attack animation (Attack_1, Attack_2, or Attack_3)
- Deals damage to player after animation delay
- Has cooldown period between attacks

## Audio Integration

The system supports:
- **Attack Sounds**: Played during attack animations
- **Chase Sounds**: Played when entering chase mode

Assign audio clips to the CrabMonsterAI component's audio arrays.

## Troubleshooting

### Common Issues:

1. **Crab not moving**:
   - Ensure NavMesh is baked in the scene
   - Check that the crab has a NavMeshAgent component
   - Verify the crab is placed on the NavMesh

2. **Animations not playing**:
   - Check that animation parameter names match in the Animator Controller
   - Ensure the Animator component is assigned to CrabMonsterAI
   - Verify animation triggers and bools are set up correctly

3. **Player not taking damage**:
   - Ensure PlayerHealth component is on the player GameObject
   - Check that player reference is assigned in CrabMonsterAI
   - Verify attack range settings

4. **Detection not working**:
   - Check detection range settings
   - Ensure player layer is set correctly
   - Verify line-of-sight raycast settings

## File Structure

```
Assets/Scripts/
??? PlayerHealth.cs          # Player health management
??? CrabMonsterAI.cs         # Main AI behavior system
??? CrabMonsterSpawner.cs    # Spawns multiple crabs in scene
??? CrabMonsterSetup.cs      # Helper for prefab configuration
```

## Performance Notes

- The AI uses NavMesh for pathfinding (efficient)
- Detection checks are optimized with distance and line-of-sight
- Attack cooldowns prevent spam
- Wandering uses simple waypoint selection

## Customization

You can easily customize:
- Damage values and health amounts
- Movement speeds and ranges  
- Attack cooldowns and patterns
- Animation parameter names
- Audio clips and effects
- Spawn patterns and locations

The system is designed to be modular and extensible for your specific game needs.