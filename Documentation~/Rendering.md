# Chart Rendering

The *Rendering* process is simply a term for playing the [`Chart`](../Runtime/Scripts/Chart.cs), which consists of instantiating playable and executing the non-playable event, all in the respective timing of the events.  

[`ChartRenderer`](../Runtime/Scripts/ChartRenderer.cs) has a property called `Position` similar to the [`RhythmEvent`](../Runtime/Scripts/RhythmEvent.cs) that continuously advances as the render process continues. 
This concept is equivalent to the position of an audio playback.  

When the `ChartRenderer` playback position reaches a particular `RhythmEvent` position, that *event* is considered to have reached the *perfect point* or *trigger point*. 
Depending on the type of event, a BPM change event should be executed, a background sound event should be played, and a playable note should reach the perfect position in the Judgment line.

To start rendering the `IChart`, you must implement the `ChartRenderer` or `ChartRenderer<T>` class. Unlike every other component, there's no default implementation. You'll need to provide the implementation on your own.
In the simplest form, here's an example of `ChartRenderer` Implementation:

```csharp
    // Let's assume our rhythm game is lane-less
    class HyperNoteEvent : RhythmEvent.Note<float>
    {
        // Let's assume a note with sound attached to it is a background note
        // This means our game is key-sound-less
        public override bool Playable => string.IsNullOrEmpty(SampleID);
    }
    
    class HyperMap : Chart
    {
        public string NoteDesigner { get; set; }
    }
    
    class HyperNote : MonoBehavior
    {
        // ... Your Behavior scripts that attached to every "note" GameObjects
    }
    
    public class HyperMapRenderer : ChartRenderer<HyperMap>
    {
        // A prefab representing the note object that will appear inside your scene
        public GameObject note;
        
        protected override void Awake()
        {
            base.Awake();
    
            // Configure "Channels", Note that even lane-less rhythm games still have to configure the Channel
            ConfigureChannel<float>();
        }
    
        protected virtual void Start()
        {
            // Start the rendering process once the script is started
            HyperMap myHyperMap =  // .. Get or load the chart
            Render(myHyperMap, Difficulty.Hard, new RenderConfig()
            {
                Speed                  = 2.0f // Be aware that the ChartRenderer.Offset doesn't account Config.Speed
                RenderDelay            = 2,   // Add 2 Measures before starting the chart
                InstantiationProximity = 5    // The instantiable note will be instantiated when it is within 5 Measures away from the current renderer position
            });
        }
    
        public override Object Instantiate(RhythmEvent ev)
        {
            // Instantiate only note event (including background sound note)
            UnityEngine.Object target = null;
            if (ev is HyperNoteEvent hyperEv)
            {
                // Instantiate the note
                target     = Instantiate(note);
                var script = target.GetComponent<HyperNote>();
        
                // Initial position should be outside the camera
                target.transform.position = new Vector2(hyperEv.Channel, (ev.Position - Position) * Config.Speed);
        
                // Configure the note script
                script.EventState        = GetEventState(ev);
                script.NoteSpeed         = Config.Speed;      // Speed needs to be applied to the note by yourself
                script.QueryRenderState += () => RenderState; // Utility function to get the captured RenderState at the beginning of the calling frame
        
                // Attach the sound into the note object if it's a background note
                if (!hyperEv.Playable)
                    script.AudioClip = YourCustomSoundBank.Get(hyperEv.SampleID);
            }
            else
            {
                target = // Instantiate other stuff if needed..
            }
    
            return target;
        }
    }
```

The implementation of `ChartRenderer` must be attached to a GameObject in your scene. 
You can then start the rendering process by calling the `Render<T>(chart, difficulty, config)` method where the type parameter of `T` is the type of difficulty to specify the difficulty of the chart you wish to render.  

## Configuring Render Config, Judgment, Scores, and Channels / Lanes

Before you start the render process, several components can be configured.

> [!Note]
> The provided systems, such as Judgment, Score Stats, and Front Buffer, are simple yet powerful features to build a complex system in your game.
> However, you can skip these steps if you plan to build these features from scratch.

> [!Important]
> Each of these systems can only be configured once.  
> Subsequent calls of the configuration method will override the existing configured system.
>
> The default configuration is configured in the `Awake()` method.

### Render Config

Represents an optional config that can be passed when calling the `Render()` method. By default, the config class has 3 properties: 

- `Speed`: Specifies the speed. It will not affect the music timing or `ChartRenderer.Postion`. Therefore, you need to apply it yourself.
- `RenderDelay`: Specifies the delay before the render starts. It uses the *Event Position* format and **not in seconds**.
- `InstantiationProximity`: Specifies the distance instantiation threshold of when the instantiable event will be instantiated. It uses the *Event Position* format and **not in seconds**.
  Use `0` to instantiate all the instantiable events at the beginning of music (Not recommended).

You can also inherit this class to create your own config, which can be passed to the render function and obtained later during the rendering process.

### Configuring Judgment

The community often refers to `Judgment` as an acceptable time window for user input that determines the timing accuracy against a particular note.
The `ChartRenderer` has pre-configured `Judgment` using the `RhythmCore.Accuracy` enum and pre-determined time window in seconds, but you can override this configuration or ignore it if you plan to build the judgment on your own.

To start configuring the judgment, call the `ConfigureJudgment<TAccuracy>()` method, where the `TAccuracy` is the type of enum that represents available accuracies in your game.
You need to pass the configurator function to this method and call `Register` with an evaluator function that will be called to determine whether a particular *event* is within the time window of the associated accuracy.

> [!Warning]
> The `RhythmEvent` will be associated with the lowest accuracy (e.g., *miss*) when none of your judgment evaluators return `true`.  
> Therefore, it is important **not** to leave any timing gap between different levels of accuracy when defining your custom judgment evaluator!

```csharp
  enum HyperAccuracy
  {
      None    = 0, // Used when the event is still far ahead of the current renderer position, which is out of range from the judgment time window
      Justice = 1,
      Great   = 2,
      Bad     = 3,
      Miss    = 4
  }

  public class HyperMapRenderer : ChartRenderer<HyperMap>
  {
      protected override void OnRender()
      {
          ConfigureJudgment<HyperAccuracy>(judgment =>
          {
              // For example, we're implementing a time window that varies depending on the selected difficulty
              var difficulty = GetDifficulty<RhythmCore.Difficulty>();
              float modifier = difficulty switch
              {
                  Difficulty.Easy   => 1.00f,
                  Difficulty.Normal => 0.75f,
                  Difficulty.Hard   => 0.50f,
              }

              // Register HyperAccuracy.Justice judgment rule
              judgment.Register(HyperAccuracy.Justice, (ev, state) =>
              {
                  // Configure the judgment rule
                  const float justiceWindow = 0.25f;                                                                 // 0.25 seconds time window
                  float distance = ev.Position - state.Position;                                                     // Calculate the distance between note and renderer current position
                  float latency = TimingUtility.PositionToSeconds(distance, state.Tempo.Bpm, state.Tempo.Signature); // Convert the distance into seconds
  
                  // Return a tuple, a short-hand of creating Judgment<T>.Result
                  // The first value indicates whether the input is recognized as HyperAccuracy.Justice
                  // The second value denotes the latency between the note timing and user input 
                  return (latency <= justiceWindow * modifier, latency);
              });
  
              // Do the same for the rest of HyperAccuracy enum members here..
  
              // Return the judgment
              return judgment.Build(
                  HyperAccuracy.Perfect, // The "Highest" accuracy in your accuracy enum
                  HyperAccuracy.Miss,    // The "Lowest" accuracy in your accuracy enum
                  HyperAccuracy.None     // The default accuracy when the event is still far ahead of the current renderer position and not ready to judge yet
              );
          });
      }
  }

  // ...
  // When receiving the user input, use the following code to evaluate the user input timing against a particular event timing:
  var rhythmEvent = ..                                     // Typically from the *front buffer*
  var judgment = GetJudgment<HyperAccuracy>();             // Get the configured judgment with HyperAccuracy
  HyperAccuracy accuracy = judgment.Evaluate(rhythmEvent); // It will use the last captured `RenderState.Position` as the user input timing.

  Debug.Log(accuracy);
```

Alternatively, you can use the built-in evaluator by calling the `judgment.Register(accuracy, seconds)` method instead, the `seconds` parameter represents the timing window of the specified `accuracy` in seconds.  

You can attempt to judge an *event* by calling `Judge<T>(RhythmEvent, hits)` where the `T` type parameter is the type of accuracy.
In addition to judging the event, it will update the score stats. For example, when your game receives user input, use the following code to judge the *event*:

```csharp
  RhythmEvent ev = ... // Get your event
  if (Judge<HyperAccuracy>(ev))
  {
      // Judgment connect, you can get the details of the judgment via EventState
      var state = GetEventState(ev);

      Debug.Log(state.Completed);                         // Print true
      var result = state.GetJudgeResult<HyperAccuracy>(); // The details of judgment result
  }
```

### Configuring Score State

The package also provides a convenient way to implement and track the user score.
The `ChartRenderer` has pre-configured `ScoreState` using the `RhythmCore.Accuracy` enum and pre-determined score point, but you can override this configuration or ignore it if you plan to build the scoring system on your own.

Call the `ConfigureScoreStats<TAccuracy>` where the `TAccuracy` is the type of enum that represents available accuracies in your game.

```csharp
    enum HyperAccuracy
    {
        None    = 0, // Used when the event is still far ahead of the current renderer position, which is out of range from the judgment time window
        Justice = 1,
        Great   = 2,
        Bad     = 3,
        Miss    = 4
    }

    public class HyperMapRenderer : ChartRenderer<HyperMap>
    {
        protected override void OnRender()
        {
            ConfigureScoreState<HyperAccuracy>(score =>
            {
                // Register the scoring rule for `HyperAccuracy.Perfect`
                score.Register(HyperAccuracy.Justice, (chart, difficulty) =>
                {
                    // For example, we're implementing a score modifier that varies depending on the selected difficulty
                    int modifier = difficulty switch
                    {
                        Difficulty.Easy   => 1,
                        Difficulty.Normal => 2,
                        Difficulty.Hard   => 3,
                    }

                    // If you want a unique scoring system for a specific chart, you can check and process the `chart` parameter.

                    // Perfect base point is 100
                    return 100 * modifier;
                });
    
                // Do the same for the rest of the HyperAccuracy enum members here..

                // Specifies which accuracies that causing the player combo to break (e.g. Getting Bad and Miss will reset the player combo)
                score.SetComboBreakers(HyperAccuracy.Bad, HyperAccuracy.Miss);
    
                // Return the score object
                return score;
            });
        }
    }

    // ...
    // Query player score:
    int perfect = score.GetStatsFor(Accuracy.Perfect);
  
    // Increase the Great count by 1 (you can specify the number of increments with the 2nd parameter).
    score.Update(HyperAccuracy.Great);
```

### Configuring Channel

Finally, you can configure the channel enumeration to the `ChartRenderer`. Specifying channel is required to generate the *Front Event List*, also known as *Front Buffer*.
The *Front Buffer* represents events that have not been judged and are closest to the "judgment line", which determines the events that will be judged when the game receives user input.

It should be noted that not all Rhythm Game follows this concept. For example, Non-Standard VSRG games may allow the player to click or tap any note in any order regardless of the surrounding note's timing order. 
The user input doesn't have to correspond with the closest note to the judgment line.  

This is the opposite of the standard VSRG game, where the game will always judge the closest event to the judgment line (the front buffer) that follows the sequence of the note order. 
As such, you can skip this step if you're not building a rhythm game with the Front Buffer mechanic.

To configure the channel, use `ConfigureChannel<TChannel>()` where the `TChannel` is the channel type. This type parameter follows exactly the same rule as the `RhythmEvent.Note<TChannel>` type parameter. 
You can get the Front Event List by calling the `GetFrontEventList<TChannel>()` and then `GetFrontEventFor(TChannel)` or `GetFrontEvents<TNote>()` to get the events.

```csharp
var frontBuffer = GetFrontEventList<float>();

// Get the front buffer for Channel.K2. It will return null if there are no more events for the specified channel
var ev = frontBuffer.GetFrontEventFor(Channel.K2);

// Alternatively, you can get an array of events that represents the front events in all channels
// Note that it will return null if the Front Event doesn't match with the TNote type parameter.
var array = frontBuffer.GetFrontEvents<RhythmEvent.Note<Channel>>();
```

## Event Behavior and Instantiation

The instantiation of an *event* is triggered when the distance between the event and renderer position is within the `RenderConfig.InstantiationProximity` range or when the `RenderConfig.InstantiationProximity` is set to `0`.
For this purpose, you are required to override the `Instantiate(RhythmEvent)` abstract method.  

The framework doesn't know what kind of rhythm game you're building, so providing instantiation implementation out of the box is impossible. 
Your implementation of `ChartRenderer` should typically hold Prefabs references representing various instantiable event types in your game.

Each object should have a script attached that represents your event behavior. This behavior depends on the implementation of your object management. So, you'll need to implement it on your own.

```csharp
    public class HyperTapNote : MonoBehaviour
    {
        // Let us assume this is a Lane-less VSRG, and the perfect point is when the note transform y coordinate reaches 4f
        private const float PerfectPoint = 4f;

        // Keeping track of the Event and its state
        public EventState EventState { get; set; }

        // Keeping track of the Note Speed
        public float NoteSpeed { get; set; }

        // Reference to the renderer
        // So you can calculate the distance between the renderer position and the event position
        public HyperMapRenderer HyperRenderer { get; set; }

        // .. Or if you don't want to have a direct dependency to the renderer
        // Provide a way to query the Renderer Position in the current frame
        public Func<RenderState> QueryLatestRenderState;

        public void Update()
        {
            // Update the position of each frame, scrolling down to the PerfectPoint
            float travel = (EventState.Event.Position - HyperRenderer.Position) * NoteSpeed;
            transform.position = new Vector2(transform.position.x, PerfectPoint.y + travel);

            // Destroy the object once the event is judged
            if (EventState.Completed)
                Destroy(gameObject);
        }

        // The rest of your implementation..
    }
```

> [!Important]
> In the above example, the note individually moves based on the `HyperMapRenderer.Position`.
> Alternative approach would be introducing a Manager object and updating all the note objects within a single frame.

Once you have all the prefabs required for the notes and have the script attached, you can instantiate the event from your `ChartRenderer`.
Here's an implementation example of what an instantiation code would look like:

```csharp
    class HyperTapEvent : RhythmEvent.Note<float>
    {
        // Your tap event implementation here..
    }

    class HyperLongEvent : RhythmEvent.Note<float>
    {
        // Your long note event implementation here..

        // For example, it has a Length property which represents the length of your long note.
        public int Length { get; set; }
    }
    
    public class HyperMapRenderer : ChartRenderer<HyperMap>
    {
        // Declare every prefab that represents your instantiable events
        public UnityEngine.GameObject tapNote;
        public UnityEngine.GameObject longNote;

        // ... etc
        
        public override UnityEngine.Object Instantiate(RhythmEvent ev);
        {
            // Instantiate your event based on its types
            UnityEngine.GameObject obj;
            if (ev is HyperTapEvent tap)
            {
                  // Instantiate the tap note
                  obj = Instantiate(tapNote);
                  var component = obj.GetComponent<HyperTapNote>();
          
                  // Configure the initial transform of your event. The code below is an example of a Lane-less VSRG note
                  // The `Channel` represents the X position, and the Y position is based on the distance between the renderer position and its position (also speed)
                  obj.transform.position = new Vector2(tap.Channel, (ev.Position - Position) * Config.Speed);
          
                  // Configure the tap note
                  component.EventState = GetEventState(ev);
                  component.NoteSpeed  = Config.Speed;      // Speed needs to be applied to the note by itself and not the renderer
                  component.Renderer   = this;              // The captured RenderState at the beginning of this frame
            }
            else if (ev is HyperLongEvent long)
            {
                // Your long note implementation here..
            }

            return obj;
        }
    }
```

The `UnityEngine.Object` reference can also be obtained from the `EventState.Object`.
Comparing `EventState.Object` against `null` is expensive; instead, you can check the `EventState.Instantiated` flag to check whether the event is instantiated.

> [!Important]
> #### Tracking Component
> The `Instantiate` abstract method returns `UnityEngine.Object` and **not** `UnityEngine.GameObject`.  
> It is possible to return the Component. Therefore, you can use it to track the Component instead of `GameObject`.
>
> #### Object Lifecycle
> If you have a Manager object that manages all important Unity objects' lifecycles, you can forward the instantiated object to that manager.  
> Note that the framework doesn't require you to adhere to any object management pattern.  
>
> However, it is essential to note that the `ChartRenderer` will **not** destroy the instantiated object automatically unless you implement it.

## Non-Playable Event Execution

The execution of a non-playable event is triggered when the distance between the event and renderer position reaches the **trigger point**.
`Execute(RhythmEvent)` method can be overridden to provide execution logic for your custom non-playable event.

```csharp
    public class HyperCameraEvent : RhythmEvent
    {
        public override bool Instantiable => false;
        public override bool Playable => false;

        // Define the start position for the camera movement
        public Vector3 Start { get; set; }

        // Define the finish position for the camera movement
        public Vector3 Finish { get; set; }

        // The effect duration
        public float Duration { get; set; }
    }

    public class HyperMapRenderer : ChartRenderer<HyperMap>
    {
        public override void Execute(RhythmEvent ev);
        {
            // Call the base method to handle RhythmEvent.Tempo event
            base.Execute(ev);

            if (ev is HyperCameraEvent cam)
            {
                // Get the state of the camera event
                var state = GetEventState(cam);

                // Calculate the camera position
                float end = cam.Position + cam.Duration;
                Camera.main.transform.position = Vector3.Lerp(cam.Start, cam.Finish, Position / end);

                // Mark the event as completed when the entire movement event is applied to the camera
                if (Position >= end)
                    state.Complete();
            }
        }
    }
```

> [!Note]
> Background music could be considered a Non-Playable Event.  
> The best implementation will depend on the sound engine you use for the game.
>
> For instance, a sound engine like [FMOD](https://www.fmod.com) allows you to build your sound system entirely by code.
> Making it possible to define Background music as a non-instantiable and a non-playable event.
>
> On the other hand, playing sounds with Unity [AudioSource](https://docs.unity3d.com/ScriptReference/AudioSource.html) will require the `AudioSource` component attached to the `GameObject`.
> A `GameObject` with `AudioSource` will strongly imply that the sound comes from the `GameObject` associated with the note event.
