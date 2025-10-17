# Enhanced Event System

The Enhanced Event System is a designer-friendly, generic-free event system that allows easy event communication throughout your Unity project without requiring knowledge of generics or complex type specifications.

## Key Features

- **No Generics Required**: Use int, string, or enum identifiers for events
- **Multiple Payload Types**: Support for void, int, bool, float, string, and EventArgs payloads
- **Designer-Friendly**: Create ScriptableObject event channels from the menu
- **Component-Based**: Use components on GameObjects to trigger and listen to events
- **Thread-Safe**: Built with thread safety in mind
- **Debugging Support**: Built-in logging and statistics
- **Unity Events Integration**: Works seamlessly with Unity Events for UI binding

## Getting Started

### 1. Create an Event Channel

1. Right-click in your Project window
2. Navigate to **Create > Ameba > Enhanced Event Channel**
3. Name your event channel (e.g., "GameEventChannel")

### 2. Trigger Events with Components

1. Add the **Enhanced Event Trigger** component to any GameObject
2. Assign your Event Channel
3. Configure the event identifier (int, string, or enum)
4. Choose payload type and set values
5. Optionally enable auto-triggering on Start or Enable

### 3. Listen to Events with Components

1. Add the **Enhanced Event Listener** component to any GameObject
2. Assign the same Event Channel
3. Configure the same event identifier as your trigger
4. Set the expected payload type
5. Configure Unity Events to respond to the event

## Event Identifiers

The system supports three types of event identifiers:

### String Identifiers
```csharp
// Simple and readable
"PlayerDeath"
"Level_Complete" 
"UI_ShowMessage"
```

### Integer Identifiers
```csharp
// Good for performance and when you have many events
1001  // Game Started
1002  // Game Paused
1003  // Level Selected
```

### Enum Identifiers
```csharp
// Type-safe and organized
public enum GameEvents {
    PlayerSpawned,
    PlayerDied,
    LevelComplete
}
```

## Payload Types

The system supports various payload types:

- **Void**: No data
- **Int**: Integer values (scores, IDs, etc.)
- **Bool**: True/false values (game state, toggles)
- **Float**: Decimal values (health, time, distances)
- **String**: Text messages
- **EventArgs**: Custom data structures

## Programmatic Usage

While designed for designers, developers can also use the system in code:

```csharp
// Subscribe to events
eventChannel.AddListener("PlayerDeath", OnPlayerDeath);
eventChannel.AddListener("ScoreUpdate", OnScoreUpdate);

// Trigger events
eventChannel.Raise("PlayerDeath");
eventChannel.Raise("ScoreUpdate", 1500);

// Custom EventArgs
var customArgs = new GenericEventArgs("Level Complete!");
eventChannel.Raise("GameMessage", customArgs);
```

## Best Practices

### Event Naming Conventions

#### String Identifiers
- Use PascalCase or snake_case consistently
- Be descriptive: `"Player_TookDamage"` not `"Damage"`
- Group related events: `"UI_ShowDialog"`, `"UI_HideDialog"`

#### Integer Identifiers
- Use meaningful ranges:
  - 1000-1099: Game State events
  - 1100-1199: Player events
  - 1200-1299: UI events
  - etc.

#### Enum Identifiers
- Group related events in the same enum
- Use descriptive names
- Consider separate enums for different systems

### Performance Considerations

- **Integer identifiers** are fastest for lookup
- **String identifiers** are most readable but slightly slower
- **Enum identifiers** provide good balance of performance and readability

### Memory Management

- Always unsubscribe from events when objects are destroyed
- Use the `Auto Unsubscribe On Destroy` option in components
- Avoid holding references to destroyed GameObjects in event handlers

## Component Reference

### Enhanced Event Trigger

**Purpose**: Triggers events on the event channel

**Key Settings**:
- **Event Channel**: The ScriptableObject event channel to use
- **Identifier Type**: Int, String, or Enum
- **Payload Configuration**: Type of data to send with the event
- **Trigger Settings**: Auto-trigger options and delays
- **Unity Events**: Called when the event is triggered

**Methods**:
- `TriggerEvent()`: Trigger with configured payload
- `TriggerEvent(int/bool/float/string)`: Trigger with custom payload
- `TriggerEventWithDelay()`: Trigger after configured delay

### Enhanced Event Listener

**Purpose**: Listens for events from the event channel

**Key Settings**:
- **Event Channel**: The ScriptableObject event channel to monitor
- **Identifier Type**: Must match the trigger's identifier type
- **Expected Payload**: Type of data expected from the event
- **Unity Events**: Different events for different payload types
- **Auto Subscribe/Unsubscribe**: Automatic lifecycle management

**Methods**:
- `Subscribe()`: Start listening for events
- `Unsubscribe()`: Stop listening for events
- `SetEventChannel()`: Change channel at runtime
- `SetEventIdentifier()`: Change identifier at runtime

## Example Scenarios

### Player Health System

**Trigger Component** (on Player):
- Event ID: `"Player_HealthChanged"`
- Payload: Int (current health)
- Trigger when health changes

**Listener Component** (on Health UI):
- Event ID: `"Player_HealthChanged"`
- Expected Payload: Int
- Unity Event: Updates health bar

### Level Completion

**Trigger Component** (on Level Manager):
- Event ID: `1001` (Level Complete)
- Payload: Bool (is victory)
- Trigger when level ends

**Listener Component** (on UI Manager):
- Event ID: `1001`
- Expected Payload: Bool
- Unity Event: Shows victory/defeat screen

### Power-Up Collection

**Trigger Component** (on Power-Up):
- Event ID: `GameEvents.PowerUpCollected` (enum)
- Payload: String (power-up type)
- Trigger on collision

**Listener Component** (on Audio Manager):
- Event ID: `GameEvents.PowerUpCollected`
- Expected Payload: String
- Unity Event: Plays corresponding sound effect

## Debugging

### Enable Debug Logging

Both the Event Channel and components have debug logging options:
- **Event Channel**: Logs event raises and listener operations
- **Components**: Log subscription status and event handling

### Context Menu Actions

- **Event Channel**: "Print Event Statistics", "Clear All Events"
- **Trigger Component**: "Trigger Event", "Test Event Configuration"
- **Listener Component**: "Subscribe", "Unsubscribe", "Test Listener Configuration"

### Inspector Information

- Event channels show listener counts for each event
- Components show subscription status and configuration validation

## Migration from Generic Event System

To migrate from the existing generic EventChannel system:

1. Create Enhanced Event Channels to replace generic ones
2. Replace generic event triggers with Enhanced Event Trigger components
3. Replace manual subscriptions with Enhanced Event Listener components
4. Update event identifiers to use the new system

The enhanced system is designed to coexist with the existing system, so migration can be gradual.

## Troubleshooting

### Common Issues

**Event not triggering**:
- Check that both trigger and listener use the same Event Channel
- Verify identifiers match exactly (case-sensitive for strings)
- Ensure listener is subscribed (check debug logs)

**Payload type mismatch**:
- Trigger and listener must use the same payload type
- Check Expected Payload setting in listener component

**Performance issues**:
- Consider using int identifiers instead of strings for frequently used events
- Avoid subscribing/unsubscribing every frame

### Debug Steps

1. Enable debug logging on Event Channel and components
2. Use "Test Event Configuration" context menu action
3. Check Unity Console for subscription and event logs
4. Use "Print Event Statistics" to see active listeners

## Advanced Usage

### Custom EventArgs

Create custom EventArgs classes for complex data:

```csharp
[Serializable]
public class DamageEventArgs : EventArgs {
    public float Damage { get; }
    public Vector3 Position { get; }
    public string DamageType { get; }
    
    public DamageEventArgs(float damage, Vector3 position, string damageType) {
        Damage = damage;
        Position = position;
        DamageType = damageType;
    }
}
```

### Runtime Event Channel Creation

```csharp
// Create event channel at runtime
var eventChannel = ScriptableObject.CreateInstance<EnhancedEventChannel>();
eventChannel.name = "RuntimeEventChannel";

// Use with components
triggerComponent.SetEventChannel(eventChannel);
listenerComponent.SetEventChannel(eventChannel);
```

### Event Channel Hierarchies

Create multiple event channels for different systems:
- **GameEventChannel**: Core game events
- **UIEventChannel**: User interface events  
- **AudioEventChannel**: Sound and music events
- **NetworkEventChannel**: Multiplayer events

This provides better organization and performance isolation.