﻿using System.Drawing;
using System.Threading;

namespace Game;

public interface IMouseInput
{
    void SetCursorPos(Point p);

    void RightClick(Point p);

    void LeftClick(Point p);

    void InteractMouseOver(CancellationToken ct);
}
