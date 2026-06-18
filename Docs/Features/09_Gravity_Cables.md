# Feature Decision Record: Gravity-Simulated Cable Rendering with Port-Wrapping Loops

## 1. Introduction and Context
In modular synthesizers, patch cables hang naturally under gravity, forming U-shapes (catenaries) between ports. Additionally, a cable's plug physically wraps around or fits snug against the circular boundary of the jack.
In this simulator, we represent cables using a **Cubic Bézier Curve**.
This feature improves cable aesthetics by:
1. Approximating physical gravity (sagging).
2. Drawing a circular loop that wraps around the jack ports at both ends of a connected cable.
3. Ensuring loops are fixed on actual ports and do not follow the dragging mouse cursor.
4. Ensuring the cable lines start and end exactly on the port boundaries (edges of the circles) rather than terminating in the middle of the ports.

## 2. Why implement it this way?
A true catenary curve is defined by the hyperbolic cosine function $y = a \cosh(\frac{x}{a})$. Calculating a true catenary on the UI thread for multiple dragging/moving cables is computationally expensive.
Instead, we use a hybrid geometry with clean visual states:
- **Connected State (`endIsPort = true`)**:
  - The cable starts at the bottom edge of the start port: $P_{start\_edge} = (start.X, start.Y + r)$ (radius $r=12$ pixels).
  - It loops clockwise in a complete circle around the start port, returning to $P_{start\_edge}$.
  - The main hanging gravity curve travels from $P_{start\_edge}$ to the bottom edge of the end port $P_{end\_edge} = (end.X, end.Y + r)$.
  - It loops clockwise in a complete circle around the end port, returning to $P_{end\_edge}$.
  - The line ends exactly on the edge of the circle and never cuts into the center hole, preserving the clean look of the jack sockets.
- **Dragging State (`endIsPort = false`)**:
  - The cable starts at the bottom edge of the start port, loops around it, and then pends down to connect directly to the mouse cursor position.
  - No loop is drawn around the mouse cursor, ensuring loops stay strictly fixed to module ports and do not float in mid-air.
- **Symmetry**: The control points and loop paths are symmetric, rendering identically regardless of the cable's insertion direction (input-to-output or output-to-input).

## 3. What It Uses
- **`System.Windows.Point`**: Represents coordinates.
- **`System.Math`**: Used for `Math.Sqrt`, `Math.Abs`, and `Math.Clamp`.
- **`System.Windows.Media.BezierSegment`**: WPF's native cubic Bézier rendering segment.
- **`System.Windows.Media.ArcSegment`**: WPF's native arc rendering segment (used to draw the loop circles).
- **`System.Windows.Media.PathGeometry`**: WPF's vector path wrapper.

## 4. What Uses It
- **`MainWindow.xaml.cs`**: Calls `CreateBezier(Point start, Point end, bool endIsPort)` in three critical interactions:
  1. `DragCable`: Draws the temporary cable following the mouse pointer (`endIsPort: false`).
  2. `OnPortMouseUp`: Instantiates the visual curve when a cable connection is completed (`endIsPort: true`).
  3. `UpdateCablesPosition`: Recalculates and updates the Bézier path when modules are dragged (`endIsPort: true`).

## 5. Detailed Breakdown of the Path Geometry

### 5.1 The Mathematical Model
```csharp
private PathGeometry CreateBezier(Point start, Point end, bool endIsPort = true)
{
    double r = 12.0; // Radius of the loop around the port (jacks are 20-25px wide, so radius is 10-12.5px)

    // Start loop points
    Point startLoopBottom = new Point(start.X, start.Y + r);
    Point startLoopTop = new Point(start.X, start.Y - r);

    // Determine target point for the gravity curve
    Point targetEndPoint = endIsPort ? new Point(end.X, end.Y + r) : end;

    // Gravity Bezier curve between the start loop bottom and target end point
    double dx = targetEndPoint.X - startLoopBottom.X;
    double dy = targetEndPoint.Y - startLoopBottom.Y;
    double distance = Math.Sqrt(dx * dx + dy * dy);

    // Gravity effect (sagging downwards)
    double horizontalFactor = Math.Clamp(Math.Abs(dx) / (distance + 0.001), 0.0, 1.0);
    double baseSag = 40.0 + (distance * 0.35) * horizontalFactor;

    // To make the cable curve look natural, the control points are pulled downwards.
    double hOffset = dx * 0.25;

    Point control1 = new Point(startLoopBottom.X + hOffset, startLoopBottom.Y + baseSag);
    Point control2 = endIsPort 
        ? new Point(targetEndPoint.X - hOffset, targetEndPoint.Y + baseSag)
        : new Point(targetEndPoint.X, targetEndPoint.Y + baseSag * 0.5); // Less dramatic pull near the dragging mouse

    var figure = new PathFigure
    {
        StartPoint = startLoopBottom,
        IsClosed = false
    };

    // 1. Loop around the start port (clockwise complete circle)
    figure.Segments.Add(new ArcSegment(startLoopTop, new Size(r, r), 0, false, SweepDirection.Clockwise, true));
    figure.Segments.Add(new ArcSegment(startLoopBottom, new Size(r, r), 0, false, SweepDirection.Clockwise, true));

    // 2. The main gravity Bezier curve
    figure.Segments.Add(new BezierSegment(control1, control2, targetEndPoint, true));

    if (endIsPort)
    {
        // 3. Loop around the end port (clockwise complete circle)
        Point endLoopTop = new Point(end.X, end.Y - r);
        figure.Segments.Add(new ArcSegment(endLoopTop, new Size(r, r), 0, false, SweepDirection.Clockwise, true));
        figure.Segments.Add(new ArcSegment(targetEndPoint, new Size(r, r), 0, false, SweepDirection.Clockwise, true));
    }

    return new PathGeometry(new[] { figure });
}
```

### 5.2 Key Visual Benefits
- **Fixed Rings**: The circular loop is only generated for port-anchored endpoints. Dragging the cable to the mouse displays a clean dangling line directly to the pointer with no floating loop.
- **Circle Edges**: The curve starts and ends at the bottom edge ($Y + r$) of the ports. The visual flow begins/ends on the circle borders and does not overlay the center jack plug connection.
