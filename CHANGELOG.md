# Changelog

A running log of changes to the project: what changed, why, and how it works.
Newest entries at the top.

---

## 2026-10-18: Classes phase 3: traversal (double jump, slide, wall run)

**New file:** `Assets/Scripts/PlayerTraversal.cs`: the slide and the wall run
**Changed files:** `PlayerMovement.cs` (double jump + helpers), `PlayerDodge.cs` (Ctrl while sprinting = slide), `ClassData.cs` + both class sheets (new Traversal settings), `PlayerClass.cs`, `PlayerRespawn.cs`, `DebugHUD.cs`

### The three new moves, in simple words
| Move | How | Stamina | What happens |
|---|---|---|---|
| **Double jump** | Press **Space again in the air** | 15 | A second, full-height jump. You get it back when you land or wall jump |
| **Slide** | **Ctrl while sprinting** | 15 | You drop to half height and shoot forward, 15% faster than you were running, slowing to walking speed over 0.6 s. You fit under low things. **Press Space during the slide** to jump out of it and keep all that speed |
| **Wall run** | Jump next to a wall while **moving along it** and **holding a direction** | 15 per second | You run sideways on the wall for up to 1.2 s, rising a little at first and then slowly sinking. **Space** = wall jump: pushes you away from the wall and up, **and gives your double jump back** |

Only the **Acrobat** has these. The Scientist's class sheet has them switched off, and Ctrl while sprinting is a normal dodge for the Scientist.

### Setup in Unity (step by step)
1. Click **Player** in the Hierarchy → **Add Component** → type `Player Traversal` → click it.
2. **Make a wall to test wall running** (your scene doesn't have a standing wall yet; `wall1` is a flat plane):
   - **GameObject → 3D Object → Cube**, and rename it `TestWall`.
   - **Position:** (6, 3, 10). **Scale:** (1, 6, 20). That's a thin, tall, long wall standing next to where you start.
   - Optional: make a second one at (-6, 3, 10) for wall-to-wall jumps.
3. **Make something low to slide under:**
   - **GameObject → 3D Object → Cube**, rename it `LowBar`.
   - **Position:** (0, 2.5, 20). **Scale:** (6, 1, 1). It floats about 2 units off the floor. You're 3 tall standing (too tall) and 1.5 tall sliding (fits).
4. **Re-bake the NavMesh** (floor1 → NavMesh Surface → **Bake**) so enemies walk around the new wall instead of through it.
5. Save the scene (**Ctrl+S**).

### How to test
| Do this | You should see |
|---|---|
| Jump, then press Space again in the air | A second jump. The debug screen shows **Air jumps left: 0** until you land |
| Sprint (Shift) and press **Ctrl** | **Sliding** on the debug screen. You shoot forward and get shorter (the green collider outline gets shorter in the Scene view; the cube itself still *looks* full size, see below) |
| Slide under the LowBar | You fit under it, and you stay low until you're out from under it |
| Slide, then press Space | You jump out of the slide with lots of speed |
| Sprint along the TestWall, jump towards it | **Wall running** on the debug screen. You run along it |
| Press Space while wall running | You jump away from the wall, and **Air jumps left** goes back to 1 |
| Let go of WASD during a wall run | You fall off |

**About the look:** the player's *box shape* (the collider) gets shorter during the slide, but the *picture* of the cube doesn't change. That's fine for now, because the real character with a slide animation comes later. To see the real shape, look at the green collider outline in the Scene view with the Player selected.

### How each piece works

**Double jump (in `PlayerMovement.cs`)**
- There's a new counter: `airJumpsUsed`. It goes back to **0 every time you touch the floor**.
- When you press Space:
  1. If you're on the floor (or just left it, which is coyote time) → **normal jump**.
  2. Otherwise, if `airJumpsUsed` is less than `maxAirJumps` (Acrobat: 1) → pay 15 stamina → **air jump**, and the counter goes up by 1.
- The jump **replaces** your up/down speed instead of adding to it. So even if you're falling fast, the double jump gives a full jump, not a tiny hop.
- After a normal jump, the coyote time is used up on purpose, so a second press in the air is always treated as an **air** jump.
- The jumping code moved into its own little method, `DoJump()`, so normal and air jumps share it.
- **New helpers for other scripts:**
  - `JumpBuffered`: "was Space pressed in the last 0.1 s and not used yet?"
  - `ConsumeJumpPress()`: the same, but it also **uses up** the press, so the wall jump can "eat" it and PlayerMovement doesn't jump as well.
  - `ResetAirJumps()`: gives the double jump back (used by the wall jump).
  - `SuppressGravity`: an on/off switch that turns gravity **completely off**. The wall run uses it because it controls up/down itself.
  - `WalkSpeed`, `SprintSpeed`, `JumpSpeed`: let other scripts read these numbers.

**Slide (in `PlayerTraversal.cs`)**
1. **Who decides between slide and dodge?** `PlayerDodge` owns the Ctrl key. When Ctrl is pressed it first asks: "are we sprinting, and can the traversal script slide?" If yes → slide. If not → normal dodge.
2. **Starting:** it pays the stamina and takes the direction you're **already moving**. It works out the start speed (your current speed or sprint speed, whichever is bigger, × 1.15).
3. **Getting short:** the Box Collider's **height is halved**, and its **centre is moved down** by the same amount, so the bottom stays on the floor. Otherwise you'd shrink towards the middle and float. Size and centre are *local* numbers (before the Player's scale), just like when we set up the collider.
4. **Moving:** movement is locked (the driving-instructor switch), and every physics step your speed is set using `Mathf.Lerp(startSpeed, walkSpeed, t)`, which slides smoothly from fast to walking speed as `t` goes from 0 to 1 over 0.6 s.
5. **Ending:** when the time is up, or you slide off an edge, or you press Space, but **only if there's room to stand up**.
6. **Room to stand (`HasRoomToStand`):** it checks the space between the top of your short body and where your head *would* be standing, using `Physics.OverlapBox` (an invisible box that lists everything inside it). If anything is in there (except your own nose), you **stay low** and keep sliding slowly until you're out from under it. Otherwise you'd stand up *inside* the LowBar.
7. **Slide jump:** pressing Space doesn't jump by itself. It just ends the slide and **leaves the Space press waiting**. PlayerMovement then sees the waiting press and does a normal jump, keeping the slide speed. Two scripts working together without doing the same job twice.

**Wall run (in `PlayerTraversal.cs`)**
1. **Can we start?** Every physics step it checks *all* of these:
   - wall run is allowed for this class, and you're **in the air**
   - nothing else is moving you (dodge, whip, slide)
   - you have stamina and aren't exhausted
   - you're moving at least 4 units per second **and** holding a direction
   - you're at least 1 unit above the floor (a ray shot straight down doesn't hit anything that close)
2. **Finding the wall (`FindWall`):** it shoots two invisible rays, to your **left** and **right** compared to the way you're moving. The right side is found with `Vector3.Cross(up, forward)`, a maths trick that gives the direction at 90° to both. A ray only counts as a wall if:
   - it's not you, and it's not an enemy (anything with Health)
   - the surface is **upright**: every surface has a **normal**, an arrow pointing straight out of it. A floor's normal points up (y = 1), and a wall's points sideways (y ≈ 0). We accept y between -0.3 and 0.3.
3. **Which way to run:** `Vector3.ProjectOnPlane(yourMovement, wallNormal)` takes your movement and **removes the part going into the wall**. What's left points **along** the wall. If almost nothing is left, you were running straight *at* the wall, not along it, so no wall run.
4. **While running:** movement is locked, gravity is switched off (`SuppressGravity`), and every step:
   - stamina drains (15 per second)
   - a ray is shot back into the wall to check it's **still there** (you might run past its end). It also updates the direction if the wall curves.
   - speed = along the wall at your entry speed, plus an up speed that goes from **+2 to -4** over the 1.2 s (a gentle arc), plus a tiny push into the wall to stay stuck to it
   - you turn to face the way you're running
5. **Falling off:** when time's up, the wall ends, you're out of stamina, you let go of WASD, or you touch the floor.
6. **Wall jump:** Space → velocity = away from the wall (8) + along the wall (60% of your speed) + up (a normal jump's speed). That sends you out at an angle. Your **double jump comes back**. The same wall can't be grabbed again for 0.5 s, so you don't stick straight back onto it, but a **different** wall can be grabbed after 0.2 s. That makes wall-to-wall jumping possible.

**Class sheet changes (`ClassData.cs`)**
New **Traversal** section on each class sheet:
| Setting | Acrobat | Scientist |
|---|---|---|
| Air Jumps | 1 | 0 |
| Air Jump Stamina Cost | 15 | 15 |
| Can Slide | ✔ | ✘ |
| Slide Stamina Cost | 15 | 15 |
| Can Wall Run | ✔ | ✘ |
| Wall Run Stamina Per Second | 15 | 15 |

`PlayerClass` hands these to PlayerMovement (air jumps) and PlayerTraversal (slide and wall run).

**Safety bits**
- Dying in the middle of a slide or wall run calls `CancelAll()`, which stands you back up, lets go of the wall and switches gravity back on.
- The whip and dodge can't start during a slide or wall run, because they check whether movement is locked.

**Debug screen**
It now also shows **Sliding**, **Wall running** and **Air jumps left: X**.

### Things to tune
- **On the Player Traversal component:** Slide Duration, Slide Boost, Wall Run Max Time, Wall Run Up Speed, Wall Run End Fall Speed, Wall Jump Push, Wall Run Min Speed.
- **On the class sheets:** stamina costs and the number of air jumps (try 2 for a triple jump!).

### Next (phase 4)
The **ult**: an ult meter that fills when you land hits and dodge attacks, then **Q** for about 8 s of automatic dodging.

---

## 2026-10-16: Classes phase 2: the Acrobat's whip (crack, pull, plunge slam) ✅ confirmed working

**New files:**
- `Assets/Scripts/PlayerWhip.cs`: the whip (all three moves)
- `Assets/Scripts/Pullable.cs`: a small "how heavy am I?" label for enemies

**Changed files:** `EnemyAI.cs` (new **Pulled** state), `ClassData.cs` (new **Uses Whip** tick box), `PlayerClass.cs` (switches weapons), `PlayerDodge.cs`, `PlayerRespawn.cs`, `Acrobat.asset` (Uses Whip ticked), `Scientist.asset` (Uses Whip unticked)

### The three whip moves, in simple words
| Button | When | Move | What happens |
|---|---|---|---|
| **Left click** | On the ground | **Crack** | The whip snaps out in a long, thin line (7 units). Every enemy along the line takes **12 damage**. If you're locked on, it aims at your target |
| **Left click** | In the air | **Plunge slam** | You stop moving sideways and **dive straight down**. When you land, everything within **3.5 units** takes **30 damage**. Then you're stuck for **0.3 s** (the price for a big hit) |
| **Right click** | Anytime you're free | **Pull** (25 stamina) | **Light enemy**: gets **yanked** to just in front of you and is **dazed for 0.6 s** (free hits!). **Heavy enemy**: it's too big to move, so **you fly to it** instead |

The whip does **less damage** than the sword (12 vs 25) because it hits from far away. That's the balance we agreed on: the Acrobat is safe at range but fragile.

### Setup in Unity (step by step)
1. Click **Player** in the **Hierarchy**.
2. **Add Component** → type `Player Whip` → click it.
3. On **Player Class**, there's a new slot called **Sword Visual**. If you have the placeholder sword (a child of the Player), drag it from the Hierarchy into this slot so it hides when you're the Acrobat. If you don't have a sword, leave the slot empty.
4. **To make a heavy enemy** (to test zipping): select an enemy → **Add Component** → `Pullable` → tick **Heavy**. Enemies *without* Pullable count as light.
5. Save the scene (**Ctrl+S**).

### How to test
| Do this | You should see |
|---|---|
| Acrobat, left click on the ground | A **brown line** (the whip) snaps out for a split second. The Console shows damage on anything it touched |
| Lock on (middle click) and left click | The whip aims at the target, even if it's a bit above or below you |
| Jump, then left click in the air | You drop straight down. The Console says `Plunge slam! Hit 2 target(s)` |
| Right click at a normal enemy | The Console says `Whip pull: yanking …`. The enemy slides to you and stands still for a moment |
| Right click at a **heavy** enemy | `… is heavy, zipping to it`, and you fly to it |
| Right click at nothing | `Whip pull: nothing to pull`. **No stamina is used** when you miss |
| Switch the class dropdown to **Scientist** | Left click is the normal sword attack again, and the whip is off |

### How each piece works

**One weapon at a time (`ClassData` + `PlayerClass`)**
- Each class sheet now has a tick box: **Uses Whip**. The Acrobat has it ticked, the Scientist doesn't.
- When the class is applied, `PlayerClass` **switches on** the weapon the class uses and **switches off** the other one (`enabled = true/false`). A switched-off script stops running its `Update`, so left click only ever does **one** thing.
- It also hides or shows the **Sword Visual** with `SetActive`. `SetActive(false)` makes an object disappear from the scene, like turning off a lamp. It's still there, just switched off.
- If a class uses the whip but the Player doesn't have the PlayerWhip script, you get a yellow warning and it falls back to the sword.

**Clicks are noticed in `Update`, used in `FixedUpdate`**
Same trick as jumping and dodging: a mouse click only exists for **one frame**, and the physics step (`FixedUpdate`) might not run in that exact frame. So `Update` writes down "left was clicked", and `FixedUpdate` reads that note and then rubs it out.

**Crack (`Crack()`)**
- It uses `Physics.SphereCastAll`. Imagine **rolling a ball** from the middle of the player, forwards for 7 units. Everything the ball touches on the way gets written down. That's the whip's hit line.
- `crackWidth` (0.5) is how fat the ball is. A fatter ball is easier to hit with.
- A `HashSet` (a list that refuses duplicates) makes sure each enemy is only hit **once** per crack, even if it has several colliders.
- **Aiming:** if you're locked on, the direction is "from me straight to the target", so it can point a bit up or down. If you're not locked on, it's straight ahead.
- The **brown line** is drawn from you to the first thing hit (or to full length if it hit nothing), then disappears after 0.12 s.

**Pull (`Pull()`)**
1. **Who to pull?** Your locked target if it's within 10 units. If you're not locked on, the closest thing **with Health** straight ahead (so it can't grab walls).
2. **Missed?** Nothing happens and **no stamina** is spent. It would feel unfair to pay for a miss.
3. **Pay** 25 stamina. Like the dodge, any stamina above 0 is enough.
4. **Light or heavy?** It looks for a `Pullable` script on the target. No script, or Heavy unticked = **light**.
5. **Light:** the spot "3 units in front of you, towards the enemy" is worked out, and the enemy is told to slide there in 0.25 s:
   - Enemies with **EnemyAI** do the sliding themselves (see the Pulled state below).
   - Things **without** EnemyAI, like the dummies, are slid by a **coroutine** (`DragObject`). It moves them a little every frame using `Vector3.Lerp`, which means "the point that's X% of the way from A to B".
6. **Heavy:** you **zip** to it. Movement is locked (`ControlsLocked`, the same driving-instructor switch the dodge uses), and every physics step your speed is set to "straight at the target, 25 units per second". You stop when you're within 3 units, or after 0.6 s as a safety net. Then 70% of the speed is cut so you don't fly past (no friction, remember).
- While pulling, the whip line **stays attached** to the enemy, so you see it being dragged.

**The enemy's new Pulled state (`EnemyAI.cs`)**
- The state machine got a new state: **Pulled**. The whip calls `Pull(destination, time, stunTime)` on the enemy.
- **Entering Pulled:** the enemy stops walking by itself, stops sliding, and its red wind-up colour is cancelled. **Pulling an enemy interrupts its attack!** That's a useful trick.
- **Being dragged:** every frame it moves towards the destination with `agent.Move`. That's important: `agent.Move` keeps the enemy **on the NavMesh**, so it can't be dragged through walls or off a cliff.
- **Dazed:** after arriving it just stands there for `stunTime` (0.6 s). Then it goes back to **Chase** (or Patrol if you're dead).
- Being pulled also tells the enemy exactly where you are, so it won't go back to patrolling.

**Plunge slam (`StartSlam()` and `SlamImpact()`)**
- **Start:** movement is locked, and every physics step your speed is set to **straight down at 30**. No sideways movement, so you land right below where you clicked.
- **Landing:** when the ground check says "on the floor", an invisible ball (radius 3.5) is placed **at your feet**, and everything inside takes 30 damage.
- **Recovery:** you're stuck in place for 0.3 s, then movement is handed back.
- **Safety:** if you somehow never land within 2 s, it triggers the impact anyway so you can't get stuck.

**What stops moves from overlapping (`CanAct()`)**
The whip only does something if you're alive, movement is on, you're not already in the middle of a whip move, nothing else has locked movement, and you're not dodging. The dodge got the same rule in reverse: no dodging while the whip is zipping or slamming. This stops two scripts from fighting over the player.

**The visible whip (`LineRenderer`)**
- A **LineRenderer** is a Unity component that draws a line between points. We give it 2 points (your middle → the end of the whip), make it brown, and make it thinner at the tip.
- It's created **in code** when the game starts, as a child object called `WhipLine` under the Player. You'll see it appear in the Hierarchy during Play mode.
- It's a placeholder. A real whip model and animation can replace it later.

**Respawn fix (`PlayerRespawn.cs`)**
Before, respawning always switched the sword attack back **on**, which would have given the Acrobat both weapons. Now it **remembers** whether the sword was on before dying and puts it back the same way.

**Gizmos** (select the Player in the Scene view): an **orange line** = how far the crack reaches, a **purple circle** = the slam area.

### Things to tune (on the Player Whip component)
- **Crack** too weak or too short? Raise **Crack Damage** or **Crack Range**. Hard to hit things? Raise **Crack Width**.
- Pulled enemies land too close or too far? Change **Pull Stop Distance**.
- Slam feels floaty? Raise **Slam Speed**. Too strong? Lower **Slam Damage** or **Slam Radius**.

### Next (phase 3)
Traversal: **double jump**, **slide** (Ctrl while sprinting) and **wall run**.

---

## 2026-10-16: Classes phase 1: class sheets, stamina, dodge roll with i-frames, debug screen ✅ confirmed working

**New files:**
- `Assets/Scripts/ClassData.cs`: the "class sheet" blueprint
- `Assets/Scripts/PlayerClass.cs`: the class picker (dropdown)
- `Assets/Scripts/Stamina.cs`: the stamina meter
- `Assets/Scripts/PlayerDodge.cs`: the dodge roll + i-frames
- `Assets/Scripts/DebugHUD.cs`: temporary numbers in the corner of the screen
- `Assets/Classes/Acrobat.asset` and `Assets/Classes/Scientist.asset`: the two filled-in class sheets

**Changed files:** `Health.cs`, `PlayerMovement.cs`, `PlayerRespawn.cs`

### The big idea, in simple words
Think of a board game where every character has a **character card** with their numbers on it: how much health, how fast they run. Our game now has the same thing:
- Each class (Acrobat, Scientist) has its own **card** (a "class sheet").
- On the Player there's a **dropdown** where you choose which card to use.
- When the game starts, the Player reads the card and copies the numbers into the scripts that need them (health into Health, speed into PlayerMovement, and so on).

So to make the Acrobat faster, you don't touch any code. You open the Acrobat card and change the number.

### The two class sheets
| | Acrobat | Scientist | What it means |
|---|---|---|---|
| Max Health | **80** | 120 | How many hits you can take |
| Max Stamina | **150** | 100 | Your "energy tank" for sprinting and dodging |
| Stamina Regen | 45 | 30 | How much energy comes back per second |
| Stamina Regen Delay | 0.8 | 1 | Seconds of rest before energy starts coming back |
| Sprint Stamina Per Second | 10 | 12 | Energy used per second while sprinting |
| Walk Speed | **7.2** | 6 | Acrobat is 20% faster (6 × 1.2 = 7.2) |
| Sprint Speed | **13.2** | 11 | Also 20% faster (11 × 1.2 = 13.2) |
| Dodge Stamina Cost | 20 | 30 | Energy used by one dodge |
| Dodge Distance | 6 | 3 | How far one dodge takes you. Acrobat rolls, Scientist sidesteps |
| Dodge Duration | 0.35 | 0.25 | How long the dodge movement takes (seconds) |
| I-Frame Duration | **0.3** | 0.15 | How long you're invincible during the dodge (seconds) |
| Dodge Recovery | 0.1 | 0.2 | Short wait after a dodge before you can dodge again |

### Setup in Unity (step by step)
1. Wait for Unity to finish loading the new scripts (the little spinner in the bottom right corner stops).
2. Click **Player** in the **Hierarchy** (the list on the left).
3. In the **Inspector** (the panel on the right), scroll to the bottom and click **Add Component**. Type `Player Class` and click it.
   - **Stamina** and **Player Dodge** get added **automatically** with it. That's what `[RequireComponent]` in the code does.
4. On the new **Player Class** component:
   - **Selected Class**: the dropdown. Pick **Acrobat**.
   - **Acrobat Data**: drag the file `Assets/Classes/Acrobat` from the **Project** window (bottom of the screen) into this slot.
   - **Scientist Data**: drag `Assets/Classes/Scientist` into this slot.
5. Click **Add Component** again, type `Debug HUD` and click it.
6. Save the scene (**Ctrl+S**).

**Tip:** you can change the **Selected Class** dropdown **while the game is playing**, and the new class takes effect straight away. That's handy for feeling the difference.

### How to test
| Do this | You should see |
|---|---|
| Press Play | Top left: the class name, a red **HP** bar and a green **Stamina** bar |
| Hold **Shift** and move | Stamina goes down. When it hits 0, the bar turns **orange** and you can't sprint until it refills to 25% |
| Press **Ctrl** while moving | You roll that way. You turn **cyan** while invincible. The Console says `I-frames ON` then `I-frames OFF` with times |
| Press **Ctrl** while standing still | You hop **backwards** (a backstep, like Elden Ring) |
| Let an enemy hit you mid-dodge | The Console says `Hit blocked by i-frames`, and your HP doesn't go down |
| Dodge with no stamina | The Console says `Dodge failed: no stamina` |

### How each piece works

**What is a ScriptableObject? (`ClassData.cs`)**
- Normal scripts (MonoBehaviours) live **on objects in the scene**, like a sticker on a box.
- A **ScriptableObject** is a **data file that lives in your project folder**, like a recipe card in a drawer. It isn't in the scene at all.
- `ClassData.cs` is the **blank card**: it says *which* numbers a class has. `Acrobat.asset` and `Scientist.asset` are **filled-in cards**.
- `[CreateAssetMenu]` adds a menu item, so you can make more cards yourself: right-click in the Project window → **Create → Game → Class Data**. That's how you'd add a third class later.

**The class picker (`PlayerClass.cs`)**
- `PlayerClassType` is an **enum**, a fixed list of choices (`Acrobat`, `Scientist`). Unity shows an enum as a **dropdown** in the Inspector.
- In `Awake` (when the game starts), `ApplyClass()` picks the card that matches the dropdown and hands each number to the right script:
  - health → `Health.SetMaxHealth()`
  - stamina numbers → `Stamina.Setup()`
  - dodge numbers → `PlayerDodge.Setup()`
  - speeds → `PlayerMovement.SetSpeeds()`
- `OnValidate` is a special Unity method that runs **whenever you change a value in the Inspector**. We use it to re-apply the class if you change the dropdown during Play mode.
- If a card slot is empty, the Console shows a yellow warning telling you to drag one in.

**Stamina (`Stamina.cs`)**
- Think of a **water tank**: sprinting and dodging pour water out, and it slowly fills back up by itself.
- **Refill delay:** the tank only starts refilling after you've **stopped using it** for a moment (`regenDelay`). Every time you spend stamina, that timer restarts.
- **Two ways to spend:**
  - `TrySpend(amount)` is for **one-off actions** like a dodge. Like Elden Ring, it works as long as you have **any** stamina left, even if the cost is bigger than what's left (it just drops to 0). It says "no" only when you're at exactly 0.
  - `Drain(amount)` is for things that **use it up over time**, like sprinting. A tiny bit is taken every physics step.
- **Exhausted:** when the tank hits 0, you're out of breath (`IsExhausted`). You can't sprint again until it refills to **25%**. Without this, sprinting would flicker on and off every frame when the tank is nearly empty.
- `Refill()` fills it completely (used when you respawn).

**Sprinting with stamina (changes in `PlayerMovement.cs`)**
- You now only sprint if **all** of these are true: Shift is held, you're actually moving, and you're not exhausted with stamina above 0. The result is saved in `IsSprinting`.
- While sprinting, `sprintStaminaPerSecond × Time.fixedDeltaTime` is drained every physics step. Physics runs 50 times a second, so 50 small pieces add up to the full per-second cost.
- If the Player has **no** Stamina script, sprinting is free (like before).
- New things other scripts can read: `IsGrounded` (on the floor?), `IsSprinting`, and `GetMoveDirection()` (now public, so the dodge knows which way you're pressing).
- **`ControlsLocked`:** a new on/off switch. When it's **on**, PlayerMovement stops doing walking, turning and jumping, and **only does gravity**. It's like a driving instructor taking over the wheel for a moment. The dodge turns it on so it can move the player itself, then turns it off again.

**The dodge (`PlayerDodge.cs`)**
1. **Pressing Ctrl** (in `Update`) only *remembers when* you pressed it. The actual dodge happens in `FixedUpdate`, the physics step, because it moves a Rigidbody. If you press Ctrl slightly too early (up to 0.15 s before you're allowed to dodge), it still counts. This is called an *input buffer*.
2. **Can I dodge?** Only if you're alive, standing on the ground (no air dodges), movement isn't switched off, and the recovery time from the last dodge is over.
3. **Pay for it:** it tries to spend the stamina. With 0 stamina, the dodge doesn't happen.
4. **Which way?** The direction you're pressing (relative to the camera). If you press nothing, it goes **straight backwards** (a backstep).
5. **Go!** It turns on `ControlsLocked`, makes you invincible for `iFrameDuration` seconds, and every physics step sets your speed to `distance ÷ duration`. For example, Acrobat: 6 ÷ 0.35 ≈ 17 units per second.
6. **Finish:** after `dodgeDuration` it hands control back to PlayerMovement and **cuts 70% of the speed**, so you don't slide on forever (the player has no friction, remember?). Then the recovery timer starts.
- While locked on, you keep facing the enemy during the roll, so A/D dodges sideways around it.

**I-frames (changes in `Health.cs`)**
- **I-frames** means *invincibility frames*: a short moment where hits pass through you.
- Health now remembers a **time**: "invincible until 12.85 seconds". `IsInvulnerable` is simply "is the clock earlier than that time?".
- **Why a time and not just on/off?** Later, several things will give i-frames (dodge, ult, getting hit). With on/off, one could switch the others off by accident. With a time, `MakeInvulnerable()` only ever makes the window **longer**, never shorter.
- In `TakeDamage`, if you're invincible the hit is **ignored**. It prints `Hit blocked by i-frames` and fires a new event, **`onHitBlocked`**. The Acrobat's ult meter will use that event later ("fill the meter when you dodge an attack").
- New helpers: `SetMaxHealth()` (used by the class picker), and `Revive()` now also clears invincibility.

**The i-frame debug notification (in `PlayerDodge.cs`)**
- Every frame it checks: "is the player invincible *now*, and was it different *last* frame?" Only when it **changes**, it:
  - prints `I-frames ON at 12.55s` or `I-frames OFF at 12.85s` in the Console (cyan text). The difference between the two times shows exactly how long the window was.
  - turns the player **cyan** (ON) or back to its normal colour (OFF).
- It's watching Health, not the dodge, so it will **also** show i-frames from the ult later, for free.
- You can switch it off with **Show I Frame Debug** on the Player Dodge component.

**The debug screen (`DebugHUD.cs`)**
- `OnGUI` is an old, simple Unity way to draw boxes and text **on top of the game**. It's perfect for quick debug info, but not for the real game UI (that comes in a later step).
- It shows: class name, an **HP bar** (red), a **Stamina bar** (green, or **orange** when exhausted), and what you're doing right now: **I-FRAMES** (cyan), Dodging, Sprinting, In air.
- `Mathf.Ceil` rounds **up** (0.4 → 1), so the bar never says 0 while there's still a little stamina.
- You can hide it with **Show** on the component.

**Respawn (changes in `PlayerRespawn.cs`)**
- After respawning, stamina is refilled, and `ControlsLocked` is switched off in case you died in the middle of a dodge.

### How the class sheet files were made
`Acrobat.asset` and `Scientist.asset` were written as files directly, instead of being created through the Create menu. To make that work, `ClassData.cs.meta` was given a fixed ID that the two card files point to. You can still make new cards normally with **Create → Game → Class Data**.

### Next (phase 2)
The whip: **left click** = crack (mid-range, about 7 units), **right click** = pull, **left click in the air** = plunge slam. The whip replaces the placeholder sword for the Acrobat.

---

## 2026-10-16: Fixed floors "falling" and the enemy bobbing up and down (done in the Unity editor) ✅ confirmed working

**Objects:** `floor1` and the enemy `Dummy (2)` in `SampleScene`

### The bug, in simple words
When the game started, every floor and wall that wasn't the main floor slowly sank away. The enemy kept dropping down, popping back up, and dropping again, until the player killed it.

### First, some words we need

**What is a NavMesh?**
Think of the floor as a big room. A NavMesh is an **invisible map drawn on the floor** showing *"you are allowed to walk here"*. Enemies can't see the room like we can, so they read this map to find their way around. In the Scene view, this map is the **blue overlay**.

**What does "Bake" mean?**
Baking is **drawing that map**. Unity looks at everything in the scene and works out where there is flat ground with enough space to walk. It's called baking because you do it once, before the game starts, like baking a cake. The map doesn't change on its own while the game runs. If you move things around, you have to bake again so the map is up to date.

**NavMesh Surface vs NavMesh Agent (two different things with similar names!)**
- **NavMesh Surface** = the **map maker**. It goes on the **floor**. It's the thing with the Bake button.
- **NavMesh Agent** = a **walker that reads the map**. It goes on **enemies only**. It's the thing that moves a character around using the map.

**What is a Layer?**
A layer is like a **coloured sticker** you put on objects to put them into groups. We gave every enemy an `Enemy` sticker. Then other parts of Unity can say "do this for everything *except* things with the Enemy sticker". You can see an object's layer at the very top of the Inspector, next to its name.

### Why it happened

**Problem 1: the floor had a NavMesh Agent (a "walker") on it.**
- `floor1` had the right component (**NavMesh Surface**, the map maker), but *also* a **NavMesh Agent** by accident.
- That told Unity: *"this floor is a character that walks around."* So every frame, Unity moved the floor around like a character.
- The player, the enemy and the camera were all standing on `floor1`, so they got carried along with it.
- The other floors and walls (`floor2`, `floor3`, `wall1`) **can't fall**, because they have no Rigidbody. They only *looked* like they were sinking because **you were being moved upwards with floor1**. It's like being in a lift: when the lift goes up, the outside world seems to go down.
- On top of that, the floor's "walker" was **40 units wide** (radius 5 × the floor's scale of 8). The enemy's own agent kept trying to steer around this giant "character" it was standing inside.

**Problem 2: the enemy was drawn into its own map.**
- When baking, the map maker looked at **every** object, including the enemy, because Include Layers was set to everything.
- So the map maker thought the enemy was a **box-shaped obstacle**: a pillar. It cut a hole in the map where the enemy stands, and it also drew a tiny walkable patch **on top of the enemy's own head**, because the top of a box is flat ground too.
- When the game started, the enemy looked for the closest bit of map to stand on. That could be the patch on **its own head**. So it was trying to stand on top of itself while also standing over the hole. That made it fall, pop back up, and fall again.

### The fix, step by step
1. **Click `floor1`** in the **Hierarchy** (the list of objects on the left).
2. Look at the **Inspector** (the panel on the right). Scroll down until you find the **Nav Mesh Agent** component.
3. Click the **⋮** (three dots) in the top right corner of that component → **Remove Component**.
   - ⚠️ Remove only the **Nav Mesh Agent**. Keep the **NavMesh Surface**!
4. Still on `floor1`, find the **NavMesh Surface** component. Inside it, look for the section called **Object Collection**. It has two settings:
   - **Collect Objects**: *which* objects the map maker looks at. Leave it as it is.
   - **Include Layers**: which *layer stickers* the map maker looks at.
5. Click the **Include Layers** dropdown (it probably says *Everything* right now) and **click `Enemy`** so its tick goes away. Now it should say *Mixed…*, which means "everything except some".
6. At the **bottom** of the NavMesh Surface component there are two buttons: **Clear** and **Bake**. Click **Bake**.
7. Check the Scene view. The blue map should cover the floor, with **no hole** where the enemy stands.
8. Save the scene (**Ctrl+S**).

### What Include Layers really does
- When you press Bake, the map maker goes around the scene and looks at objects to decide where the ground and the obstacles are.
- **Include Layers is a guest list** for that. Only objects with a ticked layer sticker are looked at. Objects with an unticked sticker are **invisible to the map maker**, as if they weren't there at all.
- We untick `Enemy` because enemies **move**. If they're baked in, the map gets a hole where the enemy was standing *at the moment you pressed Bake*. Even after the enemy walks away, the hole stays, because the map doesn't change by itself. And the enemy might get stuck in its own hole.
- **Walls and rocks *should* stay ticked.** They don't move, and we *want* holes in the map where they are, so enemies walk around them instead of trying to walk through them.
- **It only affects baking.** Unticking `Enemy` doesn't change anything else: enemies still collide, can still be hit, and can still be locked onto.

### Small extra things noticed
- The layer is called `Enemy ` **with a space at the end**. It doesn't break anything (no code uses the layer's name), but you can tidy it in *Edit → Project Settings → Tags and Layers*.
- `floor2` and `floor3` are far from `floor1` (about 79 and 78 units away), so the map can't connect them. Enemies can't walk between separate floors unless the floors touch. That's fine if they're meant to be separate areas.
- The enemy's **Base Offset of 0.5** was correct: it puts the enemy's bottom exactly on the floor.

---

## 2026-10-15: Enemy AI (state machine + NavMesh), player health and respawn

**New files:** `EnemyAI.cs`, `PlayerRespawn.cs`
**Changed:** `Health.cs` (added `Revive()`)
**Done in the editor:** installed the **AI Navigation** package (1.1.7), added a **NavMesh Surface** to the Plane and baked it.

### Setup in Unity
1. **Player:** add **Health** (Destroy On Death **unticked**) and **Player Respawn**.
2. **Make an Enemy layer:** top of the Inspector → Layer → Add Layer… → type `Enemy` in an empty slot.
3. **Enemy:** use one of the dummies, or a new Cube scaled (1.5, 3, 1.5):
   - Set its **Layer** to `Enemy`.
   - Keep **Health** (Destroy On Death ticked) and **Lock On Target**.
   - Add **Enemy AI**. A **NavMeshAgent** is added automatically.
   - Add a **Rigidbody** and tick **Is Kinematic**.
4. **Plane → NavMesh Surface:** set **Include Layers** to everything *except* `Enemy`, then click **Bake** again.
5. **NavMeshAgent → Base Offset:** if the enemy floats or sinks while walking, adjust this until its bottom sits on the floor (try 0.5).

### Why those setup steps
- **Rigidbody, Is Kinematic:** the NavMeshAgent moves the enemy, not physics. A kinematic Rigidbody tells Unity "this collider moves, but I'm moving it myself", so collisions with the player behave properly and physics doesn't fight the agent.
- **Excluding the Enemy layer from the bake:** baking treats every object as an obstacle and cuts a hole in the NavMesh where it stands. An enemy standing in its own hole can't be placed on the NavMesh, and you'd get "not close enough to the NavMesh" errors.
- **Base Offset:** the agent puts the object's *pivot* (the middle of the cube) on the floor. The offset lifts it so the cube's bottom is on the floor instead.

### How the enemy AI works (`EnemyAI`)
A **finite state machine**: the enemy is always in exactly one `State` (an `enum`). `Update` uses a `switch` to run that state's method. `EnterState()` runs things that should happen *once* when switching (setting speed, stopping the agent etc.).

| State | What it does | Switches when |
|---|---|---|
| **Patrol** | Walks between `patrolPoints` or, if there are none, to random spots within `wanderRadius` of its start, waiting `patrolWaitTime` at each | Sees the player → Chase |
| **Chase** | Runs (`chaseSpeed` 5) to where it last saw the player, using the NavMesh. Stops a bit short (`stoppingDistance`) instead of walking into them | Within `attackRange` → Attack. Lost sight for `loseSightTime` (3 s) → Search. Player dead → Patrol |
| **Attack** | **Wind-up** (0.6 s): stops, turns to face the player and fades to red as a warning. **Strike**: one hit in a sphere in front (20 damage, player only, no friendly fire). **Recover** (0.8 s): stands still, which is your chance to hit back | Then: still close → Attack again, otherwise → Chase |
| **Search** | Walks to the last seen position and turns around looking for `searchTime` (4 s) | Sees the player → Chase. Time up → Patrol |
| **Dead** | Stops and disables the agent (the object is then destroyed by Health) | — |

**Seeing the player (`CanSeePlayer`):** all three must be true:
1. Within `sightRange` (15).
2. Inside the **view cone** (`viewAngle` 120°, using `Vector3.Angle` between its forward direction and the player), **or** within `hearingRange` (3), which works even from behind.
3. **Line of sight:** a ray to the player isn't blocked by anything else.

**Getting hit** (listens to `Health.onDamaged`): a patrolling or searching enemy that gets hit immediately chases. Sneaking up and hitting it from behind wakes it up.

**Balance choices:** chase speed 5 < player walk speed 6, so you can always run away. The wind-up gives you time to back off, and the recovery gives you time to punish.

**Gizmos** (select the enemy): yellow = sight range, white lines = view cone, cyan = hearing range, red = its attack hit area.

### Player death and respawn (`PlayerRespawn`, `Health.Revive`)
- Listens to the player's `Health.onDied`. On death, it disables movement and attacks, stops sliding, and switches to Unity's normal gravity (the custom gravity lives in the disabled movement script).
- It uses a **coroutine** (`IEnumerator` + `yield return new WaitForSeconds`) to wait `respawnDelay` (2 s) without freezing the game.
- Then it moves the player back to the start position, restores everything, and calls `Health.Revive()` (full health, not dead).
- Enemies stop attacking and go back to patrolling when the player is dead.

### Known limits / next steps
- No hit reactions yet: no stagger, knockback, flash or sound. The enemy keeps winding up even while you hit it. That's the next step.
- No health bar yet. Health only shows in the Console.

---

## 2026-10-15: Dummy enemies and placeholder sword (done in the Unity editor)

**Scene:** `SampleScene`

### What changed
- **Dummy enemies:** Cubes named `Dummy` (scale about 1.5, 3, 1.5, standing on the floor about 5 units from the player), plus copies for testing target switching. Each has:
  - **Box Collider** (comes with the Cube; needed so attacks and lock-on can detect it)
  - **Health** (100, **Destroy On Death** ticked)
  - **Lock On Target**
- **Placeholder sword:** a thin Cube named `Sword` as a **child of the Player** (local position about 0.6, 0, 0.5 and scale about 0.05, 0.1, 0.5), with its **Box Collider removed**.

### How it works / notes
- The attack doesn't use the sword. Damage comes from the invisible `OverlapSphere` in `PlayerAttack`. The sword is purely visual for now.
- The sword's collider was removed because child colliders become part of the parent's Rigidbody. It would have bumped into walls and enemies as part of the player's body.
- The sword's position and scale are *local*, multiplied by the Player's scale (2, 3, 3), which is why the numbers look small.
- No Rigidbody on the dummies: they don't move. Real enemies will use a NavMeshAgent instead.

---

## 2026-10-15: Combat basics: health, player attack and Elden Ring style lock-on

**New files:** `IDamageable.cs`, `Health.cs`, `PlayerAttack.cs`, `LockOnTarget.cs`, `PlayerLockOn.cs` (all in `Assets/Scripts/`)
**Changed:** `ThirdPersonCamera.cs`, `PlayerMovement.cs`

### Controls
| Input | Action |
|---|---|
| Left click | Attack |
| Middle click (press the scroll wheel) | Lock on / release the lock |
| Scroll up / down (while locked) | Switch to the next target to the right / left |
| Shift (while locked) | Sprint, and the player faces where they run instead of the target |

### Setup in Unity (needed for it to work)
1. **Player:** add the **Player Attack** and **Player Lock On** components. Player Lock On finds the Main Camera by itself; its slot can stay empty.
2. **Dummy enemy:** GameObject → 3D Object → **Cube**, place it in front of the player (e.g. 5 units away), and set its scale to something like (1.5, 3, 1.5) so it's roughly player-sized.
   - Add **Health** and tick **Destroy On Death**.
   - Add **Lock On Target**.
3. Duplicate the dummy (Ctrl+D) a couple of times and spread the copies out to test switching targets.

### What each new script does
- **`IDamageable`** (interface): a "promise" that something has a `TakeDamage(amount, hitFrom)` method. The attack code only looks for this, so it can hurt enemies, the player, crates etc. without knowing what they are. `hitFrom` is there for knockback later.
- **`Health`**: works for **any** object (the player and enemies share it).
  - `maxHealth` (100), `CurrentHealth`, `IsDead`.
  - `TakeDamage` lowers health (never below 0), prints to the Console, and fires the **`onDamaged`** event. At 0 it fires **`onDied`**, and destroys the object if **Destroy On Death** is ticked.
  - The events are **UnityEvents**: you can hook things up to them in the Inspector, and later scripts (hit reactions, health bars) will listen to them.
- **`PlayerAttack`**: on left click (if the cooldown is over), it places an invisible sphere in front of the player (`Physics.OverlapSphere`) and damages every `IDamageable` it touches.
  - It skips the player's own colliders (body and nose).
  - It uses `GetComponentInParent`, so hitting any part of an enemy finds its Health.
  - A `HashSet` makes sure one enemy is only hit once per swing, even with several colliders.
  - Gizmo: with the Player selected, the hit sphere shows in the Scene view (yellow outline, orange flash on attack).
- **`LockOnTarget`**: marks something as lockable.
  - Every target adds itself to one shared **static list** (`All`) when it appears and removes itself when it's disabled or destroyed, so the player doesn't have to search the whole scene.
  - `LockPoint` is the middle of its collider (plus an optional offset), which is where the camera aims and the red dot sits.
  - `CanBeTargeted` is false once its Health is dead.
- **`PlayerLockOn`**: the lock-on logic.
  - **Choosing a target:** of all targets within `maxLockDistance` (20) that are **on screen** and **in line of sight** (a ray from the player isn't blocked), it picks the one **closest to the middle of the screen**.
  - **Switching (scroll):** finds the nearest valid target to the right/left of the current one *on screen*. If there isn't one, it keeps the current target.
  - **Auto-release:** when the target dies, is disabled, or gets further than `breakDistance` (25).
  - **Red dot:** drawn on the target with `OnGUI` (simple 2D drawing on top of the game). Screen Y starts at the bottom but GUI Y starts at the top, so Y is flipped.

### What changed in existing scripts
- **`ThirdPersonCamera`**
  - New `LockTarget` property, set by `PlayerLockOn` (null = normal mouse control).
  - While locked, the mouse is ignored and `LookAtLockTarget` turns the camera towards the target. `Mathf.Atan2` turns the direction into a yaw (left/right) and pitch (up/down) angle, and `Mathf.LerpAngle` smoothly turns the short way round (it knows 350° and 10° are only 20° apart).
  - Because the camera always sits *behind* the focus point, aiming at the enemy puts the camera behind the player, looking past them at the enemy.
  - New settings: **Lock On Turn Speed** (8) and **Lock On Pitch Offset** (10°, tilts down a bit to see over the player).
  - The focus point is now updated *before* the angles, so lock-on aims from the player's current position.
- **`PlayerMovement`**
  - While locked on, the player faces the **target** instead of the movement direction. Movement is still relative to the camera (which faces the target), so **W/S = towards/away from the enemy and A/D = strafe/circle around it**.
  - Sprinting while locked faces the running direction, like Elden Ring.
  - It finds `PlayerLockOn` by itself, and works fine if the player doesn't have one.

### Known limits / next steps
- The attack hits instantly on click. Later it should follow an animation (wind-up → hit → recovery).
- No hit feedback yet (sound, flash, knockback, stagger). That's the hit-reaction step, which will listen to `Health.onDamaged`.
- The dummies don't move or fight back yet. Next: the AI Navigation package, a baked NavMesh, and the enemy state machine.

---

## 2026-10-15: Made the ground check debug box visible

**File:** `Assets/Scripts/PlayerMovement.cs` (`OnDrawGizmosSelected`)

### The problem
The ground check gizmo seemed to do nothing. It was a thin 0.1-tall wireframe drawn just *under* the feet, so it sat mostly inside the floor plane and was hidden.

### What changed
- It's now a **see-through filled box with an outline**, reaching from just under the feet to 0.3 units above them, so it pokes out of the floor and is easy to see.

### How it works / how to see it
- **Debug only:** gizmos never change gameplay and never appear in a built game.
- It shows in the **Scene view** (not the Game view) when the **Player** is selected in the Hierarchy (not the `nose` child), with the **Gizmos** button on.
- **Green = grounded, red = in the air.** Outside Play mode it's always red, because the ground check only runs while the game plays.
- Tip: with the Player selected in Play mode, keep the Scene view visible next to the Game view and jump. The box should flash red in the air and turn green on landing.
- The box is a visual approximation of the check area. It's drawn taller than the real check so it can be seen.

---

## 2026-10-15: Fixed walking not working after the gravity change

**File:** `Assets/Scripts/PlayerMovement.cs`

### The bug
After the custom gravity was added, the player could jump but couldn't walk (it turned on the spot or barely moved).

### Why it happened
- The stronger gravity (about 32.7, or 49–65 with the fall multiplier) presses the player into the floor much harder than before. **Friction grows with how hard you're pressed down**, so the floor gripped the player much harder.
- Each physics step, the movement code adds at most `acceleration × 0.02 s = 0.6` of speed. Friction was removing about that much or more, so speed never built up.
- It was made worse because the fall multiplier was also active **while standing**. When resting, physics makes the vertical speed wobble just below 0, so the code thought the player was "falling" and used the even stronger gravity.

### What changed
1. **The fall multiplier only applies in the air:** the condition is now `!isGrounded && rb.velocity.y < 0f` (it was just `rb.velocity.y < 0f`).
2. **No friction on the player:** in `Awake`, a Physic Material with 0 static and 0 dynamic friction and `frictionCombine = Minimum` is created and put on the player's collider.

### How it works
- The movement code already controls speeding up (`acceleration`) and stopping (`deceleration`) itself, so the player doesn't need floor friction. Friction was only fighting the code.
- **Friction Combine = Minimum:** when two colliders touch, Unity combines their friction values. *Minimum* uses the lower one, so 0 always wins and the floor can't add friction back.
- **Bonus:** this also fixes the player sticking to walls when jumping into them (the known issue from the jumping entry).
- The material is created in code, so there's no extra asset file to set up. If you want to see or tweak it in the Inspector later, it can be moved into a Physic Material asset instead.

---

## 2026-10-15: Snappier jump, faster walking and sprinting

**File:** `Assets/Scripts/PlayerMovement.cs`

### What changed
- **Sprinting:** hold **Left Shift** to move at `sprintSpeed` instead of `walkSpeed`.
- **Faster walking:** the default walk speed went from 4 to 6.
- **Custom gravity:** the jump is now set by *how high* (`jumpHeight`) and *how fast it gets there* (`timeToApex`). The script works out its own gravity from those and turns off Unity's built-in gravity for the player (`rb.useGravity = false`).
- **Fall Multiplier** now multiplies the custom gravity while falling. The default changed from 2 to 1.5 because the base gravity is already much stronger.

### Why the jump felt floaty
- The jump used real Earth gravity (9.81). The player is 3 units tall, like a 3 m person, so a 2-unit jump at Earth gravity takes about 0.64 s just to reach the top. Real-world gravity looks slow in games. Most games use 2–4× stronger gravity.
- The old extra gravity only applied on the way *down*, so the rise was still slow.

### New / changed Inspector settings
| Setting | Default | What it does |
|---|---|---|
| Walk Speed | 6 (was 4) | Normal speed, units per second |
| Sprint Speed | 11 | Speed while holding the sprint key |
| Sprint Key | Left Shift | Which key sprints |
| Time To Apex | 0.35 | Seconds to reach the top of the jump. Smaller = snappier (minimum 0.05) |
| Fall Multiplier | 1.5 (was 2) | Gravity is this many times stronger while falling |

> **Important:** a value already saved in the scene overrides the new default in the code. In the Inspector, set **Walk Speed to 6** and **Fall Multiplier to 1.5** by hand (they were saved as 4 and 2). The new settings (Sprint Speed, Sprint Key, Time To Apex) pick up their defaults automatically.

### How it works
- **Sprint input (`Update`):** `Input.GetKey(sprintKey)` is true every frame the key is held (unlike `GetButtonDown`, which is only true on the first frame). The result is stored in `sprintHeld`.
- **Sprint speed (`FixedUpdate`):** `speed = sprintHeld ? sprintSpeed : walkSpeed`. The target velocity uses that speed, and the existing acceleration/deceleration smoothly speeds up into the sprint and slows down out of it.
- **Gravity from the jump settings (`JumpGravity`):** from physics, rising `h` units in `t` seconds needs gravity `g = 2h / t²`. With the defaults (2 units in 0.35 s), `g ≈ 32.7`, about 3.3× Earth gravity.
- **Jump speed (`HandleJump`):** the launch speed is `g × t` (≈ 11.4 with the defaults), which gets you exactly `jumpHeight` high in exactly `timeToApex` seconds.
- **Applying gravity (`ApplyGravity`, every physics step):** pushes the player down by `g`, or `g × fallMultiplier` while moving downwards. It uses `ForceMode.Acceleration`, so mass doesn't matter, just like real gravity.
- **Result:** total air time is about 0.35 s up + 0.29 s down ≈ 0.64 s, compared with about 1.1 s before.

### Tuning tips
- Still floaty? Lower **Time To Apex** (e.g. 0.3).
- Falls too hard? Lower **Fall Multiplier** towards 1.
- Want a higher jump with the same snappiness? Raise **Jump Height** only. The gravity adjusts automatically.

### Note
- Only the player uses the custom gravity. Other Rigidbodies (crates etc.) still use Unity's normal gravity.
- A future idea for the class system: sprint could drain stamina, with the Acrobat having more stamina.

---

## 2026-10-15: Jumping and ground check

**File:** `Assets/Scripts/PlayerMovement.cs`

### What changed
- Added jumping on the **Space** key (Unity's built-in "Jump" input).
- Added a ground check so the player can only jump when standing on something.
- Added extra gravity while falling, coyote time and a jump buffer to make jumping feel better.
- Added a debug box drawn in the Scene view that shows the ground check.

### New Inspector settings
| Setting | Default | What it does |
|---|---|---|
| Jump Height | 2 | How high the jump goes, in units |
| Fall Multiplier | 2 | Extra gravity while falling (1 = normal gravity) |
| Coyote Time | 0.1 | Seconds you can still jump after walking off a ledge |
| Jump Buffer Time | 0.1 | Seconds a Space press is remembered before landing |
| Ground Layers | Everything | Which layers count as floor |
| Ground Check Distance | 0.1 | How far below the feet to look for floor |

### How it works
- **Reading input (`Update`):** when Space goes down, we store the *time* it was pressed (`lastJumpPressedTime`). We don't jump here, because physics runs in `FixedUpdate`, which doesn't run every frame, so a one-frame key press could be missed.
- **Ground check (`CheckGround`, every physics step):** a thin box is slid (`Physics.BoxCast`) from the middle of the player down to just under the feet. If it hits something on `groundLayers`, the player is grounded and `lastGroundedTime` is updated.
  - BoxCast ignores colliders it *starts inside*, so it never detects the player's own collider.
  - The box is 90% of the player's width so walls next to the player don't count as floor.
  - Trigger colliders are ignored, so future pickup zones etc. won't count as floor.
- **Jumping (`HandleJump`, runs before movement in `FixedUpdate`):** you jump if Space was pressed within the last `jumpBufferTime` seconds **and** you were grounded within the last `coyoteTime` seconds.
  - Launch speed comes from physics: `jumpSpeed = sqrt(2 × gravity × jumpHeight)`. This means Jump Height is the real height reached.
  - Only the vertical speed is replaced, so walking speed is kept during the jump.
  - The press and the grounded time are then reset, so one press = one jump and you can't double jump.
- **Better falling:** while in the air and moving downwards, extra gravity of `gravity × (fallMultiplier − 1)` is added. Going up feels normal and coming down is snappier, which avoids the floaty feel.
- **Debug view (`OnDrawGizmosSelected`):** with the Player selected, a box under its feet shows in the Scene view. Green means grounded, red means in the air.

### Known issue / next step
- The player may stick to walls when jumping into them while holding forward, because of friction. Fix: a Physic Material with zero friction (Friction Combine = Minimum) on the Player's collider.

---

## 2026-10-15: Camera and Game view adjustments (done in the Unity editor)

### What changed
- The Game view and camera were adjusted because the view felt too small.

### How it works / notes
- The camera's position is set by `ThirdPersonCamera.LateUpdate` every frame, so moving the camera by hand in the Scene view does nothing. Change **Distance** and **Focus Height** on the *Third Person Camera* component instead.
- Changes made in Play mode are lost when Play stops. Note the values and enter them again afterwards.
- Game view options: *Play Maximized*, the Scale slider and the aspect dropdown (e.g. 16:9).

---

## 2026-10-15: Fixed the player levitating (done in the Unity editor)

**Object:** `Player` in `SampleScene`

### What changed
- Removed the **Capsule Collider** from the Player and added a **Box Collider** (Size 1,1,1 and Center 0,0,0, the defaults).

### Why
- The Player's mesh is a **Cube** but had a **Capsule Collider**. With the Player's scale of (2, 3, 3):
  - The cube is 3 units tall (bottom 1.5 below its centre).
  - The capsule was 6 units tall (bottom 3 below its centre).
- Physics rests the *collider* on the floor, so the visible cube hovered 1.5 units in the air.

### How it works
- Collider sizes are set *before* the object's scale is applied, the same as the mesh. A 1×1×1 Box Collider matches the 1×1×1 cube mesh, and the scale stretches both together to 2×3×3.
- Don't type the scale into the collider Size (e.g. 2,3,3). It would be scaled again and end up 4×9×9.

---

## Before this log: starting point

- `PlayerMovement.cs`: Rigidbody movement relative to the camera, with acceleration/deceleration and smooth turning to face the movement direction.
- `ThirdPersonCamera.cs`: mouse-controlled orbit camera with a limited up/down angle, smooth following and a locked cursor.
- `SampleScene`: Player (with a `nose` child), Plane floor, Directional Light, Main Camera.
