// Minimal Unity Engine stubs for CI builds — compile-time only, never shipped.
namespace UnityEngine
{
    public class Object
    {
        public static void Destroy(Object obj) { }
        public static void DontDestroyOnLoad(Object obj) { }
    }

    public class GameObject : Object { }

    public class Component : Object
    {
        public Transform  transform  { get; } = new Transform();
        public GameObject gameObject { get; } = new GameObject();
    }

    public class Behaviour : Component { public bool enabled { get; set; } = true; }

    public class MonoBehaviour : Behaviour
    {
        protected virtual void Awake() { }
        protected virtual void Start() { }
        protected virtual void Update() { }
        protected virtual void OnGUI() { }
        protected virtual void OnDestroy() { }
    }

    public class Transform : Component
    {
        public Quaternion rotation { get; set; }
    }

    public struct Vector3
    {
        public float x, y, z;
        public Vector3(float x, float y, float z) { this.x = x; this.y = y; this.z = z; }
        public static Vector3 zero    => new Vector3(0, 0, 0);
        public static Vector3 one     => new Vector3(1, 1, 1);
        public static Vector3 forward => new Vector3(0, 0, 1);
        public static float Distance(Vector3 a, Vector3 b)
        {
            float dx = a.x - b.x, dy = a.y - b.y, dz = a.z - b.z;
            return (float)System.Math.Sqrt(dx * dx + dy * dy + dz * dz);
        }
    }

    public struct Vector3d
    {
        public double x, y, z;
        public Vector3d(double x, double y, double z) { this.x = x; this.y = y; this.z = z; }
        public static double Distance(Vector3d a, Vector3d b)
        {
            double dx = a.x - b.x, dy = a.y - b.y, dz = a.z - b.z;
            return System.Math.Sqrt(dx * dx + dy * dy + dz * dz);
        }
    }

    public struct Quaternion
    {
        public float x, y, z, w;
        public Quaternion(float x, float y, float z, float w) { this.x = x; this.y = y; this.z = z; this.w = w; }
        public static Quaternion identity => new Quaternion(0, 0, 0, 1);
    }

    public struct Rect
    {
        public float x, y, width, height;
        public Rect(float x, float y, float width, float height)
        { this.x = x; this.y = y; this.width = width; this.height = height; }
    }

    public struct Vector2
    {
        public float x, y;
        public Vector2(float x, float y) { this.x = x; this.y = y; }
        public static Vector2 zero => new Vector2(0, 0);
    }

    public struct Color
    {
        public float r, g, b, a;
        public Color(float r, float g, float b, float a = 1f) { this.r = r; this.g = g; this.b = b; this.a = a; }
        public static Color yellow => new Color(1, 0.92f, 0.016f);
        public static Color white  => new Color(1, 1, 1);
        public static Color red    => new Color(1, 0, 0);
        public static Color green  => new Color(0, 1, 0);
        public static Color blue   => new Color(0, 0, 1);
        public static Color black  => new Color(0, 0, 0);
        public static Color cyan   => new Color(0, 1, 1);
        public static Color clear  => new Color(0, 0, 0, 0);
    }

    public class Texture2D : Object
    {
        public int width  { get; }
        public int height { get; }
        public Texture2D(int width, int height) { this.width = width; this.height = height; }
        public void SetPixel(int x, int y, Color color) { }
        public void Apply() { }
    }

    public class Camera : Component
    {
        public static Camera main { get; } = null;
        public Vector3 WorldToScreenPoint(Vector3 position) => Vector3.zero;
    }

    public static class Screen
    {
        public static int width  { get; } = 1920;
        public static int height { get; } = 1080;
    }

    public static class Time
    {
        public static float deltaTime { get; } = 0.02f;
    }

    public static class Debug
    {
        public static void Log(object msg) { }
        public static void LogWarning(object msg) { }
        public static void LogError(object msg) { }
    }

    public enum FontStyle { Normal, Bold, Italic, BoldAndItalic }

    public enum TextAnchor
    {
        UpperLeft, UpperCenter, UpperRight,
        MiddleLeft, MiddleCenter, MiddleRight,
        LowerLeft, LowerCenter, LowerRight,
    }

    public enum FocusType { Native, Keyboard, Passive }

    public enum EventType
    {
        MouseDown, MouseUp, MouseMove, MouseDrag,
        KeyDown, KeyUp, ScrollWheel,
        Repaint, Layout, Used, Ignore,
    }

    public enum KeyCode
    {
        None = 0, Return = 13, KeypadEnter = 271,
        Escape = 27, Space = 32, Backspace = 8,
        UpArrow = 273, DownArrow = 274, LeftArrow = 276, RightArrow = 275,
    }

    public class Event
    {
        public static Event current { get; } = new Event();
        public EventType type    { get; set; }
        public KeyCode   keyCode { get; set; }
        public bool      shift   { get; set; }
        public bool      control { get; set; }
        public bool      alt     { get; set; }
    }

    public class Font : Object { }

    public class GUIStyleState
    {
        public Color     textColor  { get; set; }
        public Texture2D background { get; set; }
    }

    public class GUIStyle
    {
        public GUIStyleState normal    { get; set; } = new GUIStyleState();
        public GUIStyleState hover     { get; set; } = new GUIStyleState();
        public GUIStyleState active    { get; set; } = new GUIStyleState();
        public FontStyle     fontStyle { get; set; }
        public int           fontSize  { get; set; }
        public TextAnchor    alignment { get; set; }
        public Font          font      { get; set; }
        public bool          wordWrap  { get; set; }
        public GUIStyle() { }
        public GUIStyle(GUIStyle other) { }
    }

    public class GUISkin
    {
        public GUIStyle label     { get; set; } = new GUIStyle();
        public GUIStyle box       { get; set; } = new GUIStyle();
        public GUIStyle button    { get; set; } = new GUIStyle();
        public GUIStyle window    { get; set; } = new GUIStyle();
        public GUIStyle textField { get; set; } = new GUIStyle();
        public GUIStyle scrollView { get; set; } = new GUIStyle();
        public GUIStyle GetStyle(string name) => new GUIStyle();
    }

    public static class GUI
    {
        public delegate void WindowFunction(int id);

        public static GUISkin skin { get; set; } = new GUISkin();
        public static void   Label(Rect pos, string text) { }
        public static void   Label(Rect pos, string text, GUIStyle style) { }
        public static bool   Button(Rect pos, string text) => false;
        public static void   DrawTexture(Rect pos, Texture2D tex) { }
        public static void   DragWindow() { }
        public static void   DragWindow(Rect pos) { }
        public static void   FocusControl(string name) { }
        public static void   SetNextControlName(string name) { }
        public static string GetNameOfFocusedControl() => "";
        public static Color  color { get; set; } = Color.white;
        public static void   BeginGroup(Rect pos) { }
        public static void   EndGroup() { }
    }

    public class GUILayoutOption { }

    public static class GUILayout
    {
        public static GUILayoutOption Width(float w)      => new GUILayoutOption();
        public static GUILayoutOption Height(float h)     => new GUILayoutOption();
        public static GUILayoutOption ExpandWidth(bool v) => new GUILayoutOption();

        public static void   Label(string text, params GUILayoutOption[] opts) { }
        public static void   Label(string text, GUIStyle style, params GUILayoutOption[] opts) { }
        public static bool   Button(string text, params GUILayoutOption[] opts) => false;
        public static bool   Button(string text, GUIStyle style, params GUILayoutOption[] opts) => false;
        public static string TextField(string text, params GUILayoutOption[] opts) => text ?? "";
        public static string TextField(string text, int maxLength, params GUILayoutOption[] opts) => text ?? "";
        public static void   Space(float pixels) { }
        public static void   FlexibleSpace() { }
        public static void   BeginHorizontal(params GUILayoutOption[] opts) { }
        public static void   BeginHorizontal(GUIStyle style, params GUILayoutOption[] opts) { }
        public static void   EndHorizontal() { }
        public static void   BeginVertical(params GUILayoutOption[] opts) { }
        public static void   BeginVertical(GUIStyle style, params GUILayoutOption[] opts) { }
        public static void   EndVertical() { }
        public static void   BeginArea(Rect rect) { }
        public static void   BeginArea(Rect rect, GUIStyle style) { }
        public static void   EndArea() { }
        public static Vector2 BeginScrollView(Vector2 scroll, params GUILayoutOption[] opts) => scroll;
        public static void   EndScrollView() { }
        public static Rect   Window(int id, Rect rect, GUI.WindowFunction func, string title, params GUILayoutOption[] opts) => rect;
    }

    public static class GUIUtility
    {
        public static int GetControlID(FocusType focus) => 0;
        public static int GetControlID(int hint, FocusType focus) => 0;
    }
}
