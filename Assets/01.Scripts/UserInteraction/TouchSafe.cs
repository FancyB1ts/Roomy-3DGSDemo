using UnityEngine;

public static class TouchSafe
{
    public static bool TryGetTouch(int index, out Touch t)
    {
        if (index >= 0 && index < Input.touchCount) { t = Input.GetTouch(index); return true; }
        t = default; return false;
    }

    public static bool TryGetTwoTouches(out Touch t0, out Touch t1)
    {
        if (Input.touchCount >= 2) { t0 = Input.GetTouch(0); t1 = Input.GetTouch(1); return true; }
        t0 = default; t1 = default; return false;
    }

    public static bool TryFindByFingerId(int fingerId, out Touch t)
    {
        for (int i = 0; i < Input.touchCount; i++)
        {
            var c = Input.GetTouch(i);
            if (c.fingerId == fingerId) { t = c; return true; }
        }
        t = default; return false;
    }
}