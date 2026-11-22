# MessageAgent

MessageAgent is a .NET MAUI notification component library built on top of [LottieAgent](../LottieAgentOfMAUI_NET10/README.md). It provides a compact interactive notification label, a details dialog, status badges, image attachments and Lottie-backed status values.

## Register services

Add a project reference to both libraries, or reference MessageAgent (which itself references LottieAgent), and register the controls in `MauiProgram.cs`:

~~~csharp
using MessageAgent.Extensions;
using SkiaSharp.Views.Maui.Controls.Hosting;

builder
    .UseMauiApp<App>()
    .UseSkiaSharp();

builder.Services.AddMessageAgentControls();
~~~

## Create a notification

The provider and message data can be declared in XAML. The library keeps the original message types available:

~~~xml
<ContentPage
    xmlns="http://schemas.microsoft.com/dotnet/2021/maui"
    xmlns:x="http://schemas.microsoft.com/winfx/2009/xaml"
    xmlns:message="clr-namespace:MessageAgent.Controls.Provider;assembly=MessageAgent"
    xmlns:models="clr-namespace:MessageAgent.Core.Models;assembly=MessageAgent">

    <message:MessageProvider
        MessageShowType="ShowByClickAndAutoHide"
        Timeout="0:0:5"
        LabelEffect="Fade"
        LabelShape="Block">
        <message:MessageProvider.Message>
            <models:Message
                Type="Error"
                IsImportant="True"
                Title="Connection failed"
                Description="The server did not respond." />
        </message:MessageProvider.Message>
    </message:MessageProvider>
</ContentPage>
~~~

Built-in types are `Error`, `Alert` and `Info`. The default label shape remains `Block`.

Set `Message.IsImportant` to mark the entire notification as important. Importance belongs to the message, not to an individual status. An unread important message can keep its notification animation looping while its content is hidden and the details dialog is closed.

## Label geometry

Set `MessageProvider.LabelShape` to choose the compact label geometry:

| Value | Appearance |
|---|---|
| `Block` | Original rounded rectangle |
| `Message` | Speech bubble with a tail |
| `Pill` | Fully rounded capsule |

The geometry and its content padding are selected through XAML converters in the provider view. Applications can select the shape directly or set a reusable `Style` in their own resource dictionary.

## Add custom message types

A C# enum cannot be extended by an application at runtime. MessageAgent therefore exposes `MessageType` as a resource-backed definition object. The built-in static definitions remain available as `MessageType.Error`, `MessageType.Alert` and `MessageType.Info`; an application can add its own definitions in any ResourceDictionary.

Declare a custom type in the application resources:

~~~xml
<ResourceDictionary
    xmlns="http://schemas.microsoft.com/dotnet/2021/maui"
    xmlns:x="http://schemas.microsoft.com/winfx/2009/xaml"
    xmlns:messageTypes="clr-namespace:MessageAgent.Core.Enums;assembly=MessageAgent"
    xmlns:lottieModels="clr-namespace:LottieAgent.Models;assembly=LottieAgent">

    <messageTypes:MessageType
        x:Key="MessageType.Success"
        Name="Success"
        DisplayName="Completed"
        AccentColor="#24875B"
        BackgroundColor="#1824875B"
        NotificationAnimationSource="Lotties/circleLable.json">
        <messageTypes:MessageType.NotificationAnimationModifiers>
            <lottieModels:LottieModifierCollection>
                <lottieModels:LottieColorModifier
                    TargetName="info contornos"
                    Color="#24875B" />
            </lottieModels:LottieModifierCollection>
        </messageTypes:MessageType.NotificationAnimationModifiers>
    </messageTypes:MessageType>
</ResourceDictionary>
~~~

Use the resource as the message type:

~~~xml
<models:Message
    Type="{StaticResource MessageType.Success}"
    Title="Completed"
    Description="The operation finished successfully." />
~~~

The type converter resolves the `MessageType.{Name}` key from `Application.Current.Resources` before falling back to the library defaults. This means an application can override a built-in definition by supplying a resource with the same key, for example `MessageType.Error`, `MessageType.Alert` or `MessageType.Info`. It can also add new keys such as `MessageType.Success`. After registering the resource, both forms work:

~~~xml
<models:Message Type="Success" Title="Completed" />
<models:Message Type="{StaticResource MessageType.Success}" Title="Completed" />
~~~

A custom definition can provide:

- `Name` and localized `DisplayName`.
- `AccentColor` and optional `BackgroundColor`.
- Optional dialog colors: `DialogSurfaceColor`, `DialogForegroundColor`, `DialogSecondaryForegroundColor`, `DialogAccentColor`, `AccentContentColor`.
- Its own `NotificationAnimationSource`.
- Its own `NotificationAnimationModifiers` collection.

### Configure colors in any notification Lottie

A notification animation is not required to use the layer name from `circleLable.json`. Set `NotificationAnimationModifiers` explicitly and add one modifier for every named element that needs a change. The collection supports multiple colors in the same animation, as well as opacity, text and transform modifiers:

~~~xml
<messageTypes:MessageType
    x:Key="MessageType.Success"
    Name="Success"
    DisplayName="Completed"
    AccentColor="#24875B"
    BackgroundColor="#1824875B"
    NotificationAnimationSource="Lotties/success.json">
    <messageTypes:MessageType.NotificationAnimationModifiers>
        <lottieModels:LottieModifierCollection>
            <lottieModels:LottieColorModifier
                TargetName="MainShape"
                Color="#24875B" />
            <lottieModels:LottieColorModifier
                TargetName="SecondaryShape"
                Color="#F2C14E" />
            <lottieModels:LottieOpacityModifier
                TargetName="Shadow"
                Opacity="0.65"
                Operation="Set" />
        </lottieModels:LottieModifierCollection>
    </messageTypes:MessageType.NotificationAnimationModifiers>
</messageTypes:MessageType>
~~~

Replace `MainShape`, `SecondaryShape` and `Shadow` with the corresponding `nm` names from the Lottie JSON. The processor applies a modifier to every JSON element with the matching name, so repeated names are supported. Add as many `LottieColorModifier` entries as the animation requires; there is no single-color limit.

The built-in `Error`, `Alert` and `Info` definitions include the library-owned color modifier for their bundled `circleLable.json` animation. Custom resource definitions never assume layer names: their animation is not recolored until `NotificationAnimationModifiers` is configured. This is true even when the custom source happens to use the same path as the bundled asset. An explicitly declared collection always takes precedence, including an intentionally empty collection.

> **API migration:** `MessageType` used to be an enum and is now a class so custom instances can be supplied by resources. Existing comparisons with `MessageType.Error`, `MessageType.Alert` and `MessageType.Info` continue to work. Enum-only operations such as `Enum.GetValues<MessageType>()` and switch case labels must be replaced with an application-owned collection or ordinary comparisons.

## Lottie status values

A Lottie status uses `LottieVisualValue`, rendered by `LottieVisualValueView`. The visual value supports source, playback speed, a normalized initial `Progress` in the range `0–1`, reverse playback and named modifier collections. Progress `0` starts at the first frame, `1` starts at the final frame, and intermediate values select any position in the animation. This replaces the former `ValueStatus`, `ImageValueStatus` and `LottieValueStatus` model names with `VisualValue`, `ImageVisualValue` and `LottieVisualValue`.

~~~xml
<models:Status Name="Sync state" ShowTitle="True">
    <models:LottieVisualValue
        Source="Lotties/sync.json"
        Speed="1"
        Progress="1">
        <models:LottieVisualValue.Modifiers>
            <lottieModels:LottieModifierCollection>
                <lottieModels:LottieColorModifier
                    TargetName="StatusIcon"
                    Color="#24875B" />
                <lottieModels:LottieOpacityModifier
                    TargetName="StatusShadow"
                    Opacity="0.8"
                    Operation="Set" />
            </lottieModels:LottieModifierCollection>
        </models:LottieVisualValue.Modifiers>
    </models:LottieVisualValue>
</models:Status>
~~~

See the [LottieAgent documentation](../LottieAgentOfMAUI_NET10/README.md#declaring-modifiers-in-xaml) for all supported modifier element types and operations.

## Override built-in icons

The library's default icons are stored in `MessageAgent/Resources/Images`. They are defaults, not fixed converter output: MessageAgent first looks for an application resource and falls back to its packaged image when no override is registered. MAUI compiles SVG source files into PNG resources, so runtime image names use the `.png` extension.

The public resource keys are exposed by `MessageAgentImageKeys`:

| Key | Runtime asset name |
|---|---|
| `MessageAgentImageKeys.Read` (`MessageAgent.Images.Read`) | `message_read.png` |
| `MessageAgentImageKeys.Unread` (`MessageAgent.Images.Unread`) | `message_unread.png` |
| `MessageAgentImageKeys.Important` (`MessageAgent.Images.Important`) | `important.png` |

Define an override in the application ResourceDictionary. The resource value can be a `FileImageSource` or another `ImageSource`:

~~~xml
<Application.Resources>
    <ResourceDictionary>
        <FileImageSource
            x:Key="MessageAgent.Images.Read"
            File="my_read_icon.png" />
        <FileImageSource
            x:Key="MessageAgent.Images.Unread"
            File="my_unread_icon.png" />
        <FileImageSource
            x:Key="MessageAgent.Images.Important"
            File="my_important_icon.png" />
    </ResourceDictionary>
</Application.Resources>
~~~

The replacement files must be included in the application's `Resources/Images` folder (or supplied through another valid `ImageSource`). An omitted key keeps the corresponding library default. Resource lookup includes merged application dictionaries.

`ReadStateConverter` uses the overridable read/unread keys. The important-message marker uses the important key and appears immediately after the read/unread icon in the compact label and details header. It is shown only when `Message.IsImportant` is `true`; it is not part of the status collection.

## MessageProvider properties

| Property | Purpose |
|---|---|
| `Message` | Notification model |
| `MessageShowType` | Whether the label is always visible, click-controlled, auto-hidden or hidden |
| `Timeout` | Auto-hide delay |
| `LabelEffect` | Label show/hide animation |
| `LabelShape` | Label geometry: `Block`, `Message` or `Pill` |
| `MaxTitleLength` | Optional explicit title truncation limit |
| `MaxStatusesOnLabel` | Maximum number of status badges displayed in the compact label |

## License

This project is licensed under the MIT License.


## Message links

A message can contain a collection of links. They are rendered in the details dialog **after the description and before image attachments**. If the collection is empty, the section is hidden.

The link model supports three destination kinds. Every link has a `SortOrder`; lower values appear first when no custom sorting rules are configured.

| Type | Properties | Activation |
|---|---|---|
| `Web` | `Path` | Opens a valid HTTP/HTTPS address with the platform launcher |
| `LocalStorage` | `Path` | Opens a file from `FileSystem.AppDataDirectory` |
| `Navigation` | `Path`, `Page` or `Window` | Navigates through a Shell route, pushes a supplied Page or opens a supplied Window |

All link visuals are represented by one polymorphic `Value` property. This uses the same visual-value model family as statuses:

- `ImageVisualValue` — an image source; used for a status image or link icon.
- `LottieVisualValue` — a Lottie source, playback settings and a `Modifiers` collection.
- `ElementVisualValue` — a custom MAUI `View` supplied by the application.

Example XAML:

~~~xml
<models:Message.Links>
    <models:MessageLinksCollection>
        <models:MessageLinksCollection.SortRules>
            <models:MessageLinkSortRule
                SortParametr="SortOrder"
                SortType="Ascending"
                SortKind="Merge" />
            <models:MessageLinkSortRule
                SortParametr="Title"
                SortType="Ascending"
                SortKind="Quick" />
        </models:MessageLinksCollection.SortRules>

        <models:MessageLink
            Type="Web"
            SortOrder="1"
            Path="https://learn.microsoft.com/dotnet/maui/"
            Title=".NET MAUI documentation"
            Description="Official .NET MAUI documentation."
            Clicked="OnMessageLinkClicked" />

        <models:MessageLink
            Type="Web"
            SortOrder="2"
            Path="https://github.com/dotnet/maui"
            Description="The source code repository.">
            <models:MessageLink.Value>
                <models:LottieVisualValue Source="Lotties/arrow.json">
                    <models:LottieVisualValue.Modifiers>
                        <lottieModels:LottieModifierCollection>
                            <lottieModels:LottieColorModifier
                                TargetName="Stroke 1"
                                Color="#24875B" />
                        </lottieModels:LottieModifierCollection>
                    </models:LottieVisualValue.Modifiers>
                </models:LottieVisualValue>
            </models:MessageLink.Value>
        </models:MessageLink>

        <models:MessageLink
            Type="Web"
            SortOrder="3"
            Path="https://dot.net/"
            Title="Custom view">
            <models:MessageLink.Value>
                <models:ElementVisualValue>
                    <models:ElementVisualValue.VisualElement>
                        <Label Text="API" FontAttributes="Bold" />
                    </models:ElementVisualValue.VisualElement>
                </models:ElementVisualValue>
            </models:MessageLink.Value>
        </models:MessageLink>

        <models:MessageLink
            Type="LocalStorage"
            SortOrder="4"
            Path="Reports/latest-report.pdf"
            Title="Latest report" />

        <models:MessageLink
            Type="Navigation"
            SortOrder="5"
            Path="//SettingsPage"
            Title="Settings" />
    </models:MessageLinksCollection>
</models:Message.Links>
~~~

### Link presentation

- `Title` is displayed when supplied. Otherwise the destination is shortened to a readable label.
- `Description` is assigned to the platform tooltip.
- Set `Value` to an `ImageVisualValue`, `LottieVisualValue` or `ElementVisualValue`. The visual-value factory selects the corresponding renderer.
- A `LottieVisualValue` plays while the pointer is over the link and accepts the same `LottieModifierCollection` used by other Lottie views.
- When `Value` is omitted for a Web link, the control attempts to load a favicon from the web host. Favicon retrieval is best-effort and requires network access.

### Link activation events

Each `MessageLink` exposes a `Clicked` event and an optional `ICommand`/ `CommandParameter`. They are invoked before the built-in destination action. This lets the application log activation, update state or perform additional work while retaining the normal open/navigation behavior.

For a C#-created link:

~~~csharp
MessageLink link = new()
{
    Type = MessageLinkType.Web,
    Path = "https://example.com",
    Title = "Example"
};

link.Clicked += (_, _) => Debug.WriteLine("Example link activated");
message.Links.Add(link);
~~~

Relative local paths are resolved against `FileSystem.AppDataDirectory`. Absolute paths are accepted only when they remain inside that directory; path traversal outside the app's writable storage is rejected.

## Collection sorting

Both `StatusCollection` and `MessageLinksCollection` sort their contents automatically. With no explicit rules, each collection sorts ascending by `SortOrder`. Updating a sortable property or a rule reapplies the collection order and raises the normal observable-collection notifications.

Each collection has its own enum for `SortParametr`: `StatusSortParametr` and `MessageLinkSortParametr`. A rule also specifies `SortType` (`Ascending` or `Descending`) and `SortKind` (`Bubble`, `Selection`, `Insertion`, `Quick`, `Merge` or `Heap`). When multiple rules are present, the first rule is the primary key and subsequent rules break ties. Sort algorithms live in the independent `SortLib` project.

Example status sorting:

~~~xml
<models:Message.Status>
    <models:StatusCollection>
        <models:StatusCollection.SortRules>
            <models:StatusSortRule
                SortParametr="SortOrder"
                SortType="Ascending"
                SortKind="Merge" />
            <models:StatusSortRule
                SortParametr="Name"
                SortType="Ascending"
                SortKind="Quick" />
        </models:StatusCollection.SortRules>
        <models:Status Name="Network" SortOrder="1">
            <models:ImageVisualValue Source="network.png" />
        </models:Status>
    </models:StatusCollection>
</models:Message.Status>
~~~

## Image carousel navigation

The details dialog uses the library asset `Lotties/arrow.json` for both side arrows. The left arrow uses the source animation; the right arrow uses the same animation with a horizontal mirror applied by a negative X scale modifier. Both are colored from the message's contrast-adjusted dialog accent (`MessageType.GetDialogAccent()`). Arrow controls are disabled visually at the first and last image.


## Override built-in Lottie animations

The library's own interface animations can be replaced through application resources, using the public keys in `MessageAgentLottieKeys`. Resource values are strings containing the replacement Lottie asset path/source. Resource lookup includes merged application dictionaries; when a key is not defined, the packaged animation is used.

| Key | Default animation |
|---|---|
| `MessageAgentLottieKeys.Arrow` (`MessageAgent.Lotties.Arrow`) | `Lotties/arrow.json` |
| `MessageAgentLottieKeys.Close` (`MessageAgent.Lotties.Close`) | `Lotties/close.json` |
| `MessageAgentLottieKeys.More` (`MessageAgent.Lotties.More`) | `Lotties/more_point.json` |
| `MessageAgentLottieKeys.Minimize` (`MessageAgent.Lotties.Minimize`) | `Lotties/minimize.json` |

For example, add a replacement to the application ResourceDictionary:

~~~xml
<Application
    xmlns="http://schemas.microsoft.com/dotnet/2021/maui"
    xmlns:x="http://schemas.microsoft.com/winfx/2009/xaml">
    <Application.Resources>
        <ResourceDictionary>
            <x:String x:Key="MessageAgent.Lotties.Close">Lotties/my_close.json</x:String>
            <x:String x:Key="MessageAgent.Lotties.Arrow">Lotties/my_arrow.json</x:String>
        </ResourceDictionary>
    </Application.Resources>
</Application>
~~~

Include replacement JSON files in the application package as MAUI assets, preserving the logical asset names used in the resource values (for example, `Lotties/my_close.json`). These keys affect the library's built-in controls (the details-dialog carousel arrows, close buttons, status overflow and collapse actions). They do not rewrite sources supplied by application-defined `LottieVisualValue` instances.

The notification animation `Lotties/circleLable.json` is intentionally not part of this global override table. It is selected per message type through `MessageType.NotificationAnimationSource`, and that exact source is used both by the compact `MessageProvider` icon and the details-dialog header. This lets different message types use different notification animations while keeping their modifiers type-specific.
