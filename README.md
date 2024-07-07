# Pull Down Dismiss Panel
A Unity UI script that implements an iOS-style pull-down-to-dismiss panel behavior with smooth, DOTween animations.

## Features

- Smooth pull-down gesture to dismiss UI panels
- Dismissal animation that continues the user's drag motion
- Customizable dismiss threshold and animation parameters
- Option to disable a parent object (e.g., canvas) when the panel is dismissed
- Respects initial visibility state set in the Unity Scene
- Drag handle can be the entire panel or a specified sub-area
- Debug mode

## Demo

[![PullDownDismissPanel Demo](https://img.youtube.com/vi/4cqL7iAOdGk/0.jpg)](https://www.youtube.com/shorts/4cqL7iAOdGk)

*Click the image above to watch the demo video on YouTube*

## Installation

1. Ensure you have DOTween installed in your Unity project.
2. Download the `PullDownDismissPanel.cs` script from this repository.
3. Import the script into your Unity project's Scripts folder.

## Usage

1. Attach the `PullDownDismissPanel` script to your UI panel GameObject.
2. In the Inspector, assign the following:
   - Panel's RectTransform (if not automatically assigned)
   - (Optional) Drag handle RectTransform if you want a specific drag area
   - (Optional) Parent GameObject to disable when the panel is dismissed
3. Adjust the following parameters as needed:
   - Dismiss Threshold
   - Animation Duration
   - Velocity Multiplier
4. Enable/disable Debug Mode for logging if needed.

## Example Code

To programmatically show the panel:

```csharp
PullDownDismissPanel panel = GetComponent<PullDownDismissPanel>();
panel.ShowPanel();
```

## Contributing

Contributions are welcome! Please feel free to submit a Pull Request.

## License

This project is licensed under the MIT License - see the [LICENSE.md](LICENSE.md) file for details.

