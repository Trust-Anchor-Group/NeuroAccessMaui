# **NotificationServiceV2 — Developer Guide**

A concise, task-oriented guide explaining what a notification *is*, how it moves through the system, what you *must* set when creating one, how deduping works, how rendering works, and how to debug issues.

---

# **1. Quick Start Checklist (Use This Every Time)**

When producing a notification:

1. **Set required identifiers**

   * `Channel`
   * `Action`
   * `EntityId`

2. **Set a stable `CorrelationId`**
   Required for dedupe between XMPP + Push. Without it, each transport creates a separate record.

3. **Choose a presentation mode**

   * `RenderAndStore` (default)
   * `StoreOnly`
   * `RenderOnly`
   * `Transient`

4. **Call `AddAsync()`**

   ```csharp
   await notificationService.AddAsync(intent, source, rawPayload, ct);
   ```

5. **If you want an OS toast**

   * **Push:** auto-rendered (if filters & presentation allow).
   * **XMPP/Local:** *you must call* `INotificationRenderer.RenderAsync()`.

6. **If you add a new action**

   * Add to `NotificationAction`
   * Add a routing case in `NotificationIntentRouter`

---

# **2. The Model (What a Notification Is)**

## **NotificationIntent** (producer → service)

Represents the logical event.

| Field            | Purpose                                             |
| ---------------- | --------------------------------------------------- |
| `Channel`        | Logical grouping (chat, contracts, petitions, etc.) |
| `Title` / `Body` | User-facing text                                    |
| `Action`         | What to open when tapped                            |
| `EntityId`       | Primary routing identifier                          |
| `CorrelationId`  | *Critical for dedupe across transports*             |
| `Extras`         | Arbitrary key/value routing or display metadata     |
| `Presentation`   | Store? Render? Both? Neither?                       |

---

## **NotificationRecord** (stored form)

Created/updated by the pipeline.

| Field             | Meaning                                        |
| ----------------- | ---------------------------------------------- |
| `Id`              | Deterministic hash from intent fields + source |
| `State`           | `New`, `Delivered`, `Read`, `Consumed`         |
| `OccurrenceCount` | How many times this same logical event arrived |
| `Source`          | `Xmpp`, `Push`, `Local`                        |
| `RawPayload`      | Debug payload                                  |
| Timestamps        | Stored automatically                           |

---

# **3. How Notifications Move Through the App**

## **3.1 Ingest Pipeline**

```
Incoming event (Push / XMPP / Local)
    ↓
Build NotificationIntent
    ↓
FilterRegistry.ShouldIgnore?
    - IgnoreStore → exit (no store, no render, no routing)
    - IgnoreRender → store only
    ↓
Compute Id (hash of intent fields + source)
    ↓
Merge?
    If existing record with same:
        - Action
        - EntityId
        - CorrelationId
      → merge + increment OccurrenceCount
    Else → create new record
    ↓
Persist? (depends on Presentation)
    Render? (depends on Presentation + caller)
    ↓
OnNotificationAdded event → UI updates
```

---

## **3.2 User Tap Pipeline**

```
User taps a notification (OS or app)
    ↓
ConsumeAsync(id)
    ↓
State = Consumed
    ↓
NotificationIntentRouter.Route(intent)
    ↓
Navigate to page
```

---

# **4. Deduplication Rules (The Important Part)**

A notification merges **only** when:

1. `Action` is the same
2. `EntityId` is the same
3. `CorrelationId` is the same
4. The computed hash (NotificationKey) matches

If any differ → new notification.

### **Practical example**

| Case                                    | Result                        |
| --------------------------------------- | ----------------------------- |
| Push + XMPP message with same stanza id | **Merge** (OccurrenceCount++) |
| Push missing CorrelationId              | **Does not merge**            |
| Two different actions for same entity   | **Never merge**               |

---

# **5. Rendering Behavior (Who Shows the OS Toast?)**

### **Summary Table**

| Source    | Auto-render? | Why                                                   | If you want a toast…                                    |
| --------- | ------------ | ----------------------------------------------------- | ------------------------------------------------------- |
| **Push**  | ✔ Yes        | Mobile OS delivers notification payloads expecting UI | Done automatically if allowed by filters + presentation |
| **XMPP**  | ✖ No         | XMPP is data-only                                     | You *must call* `notificationRenderer.RenderAsync()`    |
| **Local** | ✖ No         | Local events are app-internal                         | You *must call* `RenderAsync()`                         |

### Rules

* Storing a notification **never implies rendering** (except push).
* `Presentation=RenderOnly` still requires caller to invoke renderer.
* Filters may block render even if presentation allows.

---

# **6. Actions & Routing**

## **6.1 Routing Table**

| Action                | Opens                                  | Notes            |
| --------------------- | -------------------------------------- | ---------------- |
| `OpenChat`            | `ChatPage`                             | `EntityId` = JID |
| `OpenProfile`         | `MyContactsPage`                       |                  |
| `OpenIdentity`        | `ViewIdentityPage` / KYC               | may use extras   |
| `OpenContract`        | `ViewContractPage` / `MyContractsPage` |                  |
| `OpenToken`           | `MyTokensPage`                         |                  |
| `OpenBalance`         | `WalletPage`                           |                  |
| `OpenPetition`        | `MyContactsPage`                       |                  |
| `OpenPresenceRequest` | `MyContactsPage`                       |                  |
| `OpenSettings`        | `SettingsPage`                         |                  |

## **6.2 Adding a new Action**

1. Add enum entry to `NotificationAction`
2. Add switch case in `NotificationIntentRouter`
3. Ensure all producers set it

---

# **7. Minimal Examples**

## **7.1 XMPP Event → Notification**

```csharp
var intent = new NotificationIntent
{
    Channel = Constants.PushChannels.Messages,
    Title = RemoteBareJid,
    Body = Message.PlainText ?? string.Empty,
    Action = NotificationAction.OpenChat,
    EntityId = RemoteBareJid,
    CorrelationId = Message.Id
};

await notificationService.AddAsync(intent, NotificationSource.Xmpp, Message.Xml, ct);

// XMPP does NOT auto-render:
if (!filterRegistry.ShouldIgnore(intent, false, ct).IgnoreRender)
    await notificationRenderer.RenderAsync(intent, ct);
```

---

## **7.2 Push Payload → Notification**

Preferred: Send serialized `NotificationIntent` in `notificationIntent`.

Fallback keys:

```
channelId
myTitle
myBody
action
entityId
correlationId
silent / delivery.silent
extras...
```

Android/iOS push handlers do:

* AddAsync()
* Auto-render if allowed

---

## **7.3 Local Event**

```csharp
var intent = new NotificationIntent
{
    Channel = Constants.PushChannels.Provisioning,
    Title = "Request approved",
    Body = approverName,
    Action = NotificationAction.OpenSettings,
    EntityId = approverName,
    Presentation = NotificationPresentation.RenderAndStore
};

await notificationService.AddAsync(intent, NotificationSource.Local, null, ct);

// Local notifications require manual rendering:
var decision = filterRegistry.ShouldIgnore(intent, false, ct);
if (!decision.IgnoreRender)
    await notificationRenderer.RenderAsync(intent, ct);
```

---

# **8. Common Issues & How to Fix Them**

### **8.1 “Why didn’t it show a toast?”**

* XMPP/local → did you call `RenderAsync()`?
* Presentation = `StoreOnly`?
* Filter returned `IgnoreRender`?
* iOS: app not in a state where alerts are allowed?

---

### **8.2 “Why did two notifications merge unexpectedly?”**

Check:

* Same Action?
* Same EntityId?
* Same CorrelationId?
* Same source+payload leading to same hash?

If yes → merge is correct.

---

### **8.3 “Why didn’t two notifications merge?”**

* Wrong/missing CorrelationId
* Action mismatched
* EntityId mismatched
* Different extras/title/body may cause hash divergence

---

### **8.4 “Tapping doesn’t navigate anywhere”**

* Did you call `ConsumeAsync()` on tap?
* Is the action handled in `NotificationIntentRouter`?

---

# **9. Troubleshooting Flowchart**

```
Not showing?
   ├── Was it stored?
   │      ├── No → Presentation or filters blocked it → fix producer
   │      └── Yes → continue
   ├── Should it render?
   │      ├── Push? → auto (unless filtered)
   │      └── XMPP/Local → did you call RenderAsync?
   └── Platform allowed toast? (iOS background rules)
```

```
Merge issues?
   ├── Check CorrelationId
   ├── Check Action
   ├── Check EntityId
   └── Inspect logs for computed hash components
```

---

# **10. Summary**

**If you remember only four things:**

1. **Always set a stable CorrelationId**
2. **XMPP/local notifications NEVER auto-render**
3. **Push auto-renders, others don’t**
4. **Merge = Action + EntityId + CorrelationId match**
