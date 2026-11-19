using System.Collections.Generic;
using System.Globalization;
using UnityEngine;

// Menu, race flow (countdown, laps, checkpoints), time saving and all UI (IMGUI).
public class GameManager : MonoBehaviour
{
    enum State { Menu, Countdown, Racing, Paused, Finished }
    const int Gates = 8;

    State state = State.Menu;
    int selected;
    int laps = 3;

    Camera mainCam;
    CameraFollow follow;
    Light sun;
    Track track;
    CarController car;

    float countdownT, goFlash, raceTime, lapStart, toastT, flipT, finalTime, bestLapRace, prevSd = float.NaN;
    string toast = "";
    int lap, nextGate, respawnIdx;
    bool recLap, recTotal;
    readonly List<float> lapTimes = new List<float>();
    GUIStyle sLabel, sButton, sBig, sCard;

    void Awake()
    {
        Time.fixedDeltaTime = 1f / 60f;
        foreach (Camera c in FindObjectsByType<Camera>(FindObjectsSortMode.None)) DestroyImmediate(c.gameObject);
        foreach (Light l in FindObjectsByType<Light>(FindObjectsSortMode.None)) DestroyImmediate(l.gameObject);

        GameObject camGo = new GameObject("Main Camera");
        camGo.tag = "MainCamera";
        mainCam = camGo.AddComponent<Camera>();
        mainCam.clearFlags = CameraClearFlags.SolidColor;
        mainCam.farClipPlane = 2500f;
        mainCam.nearClipPlane = 0.2f;
        camGo.AddComponent<AudioListener>();
        follow = camGo.AddComponent<CameraFollow>();
        follow.cam = mainCam;

        GameObject sunGo = new GameObject("Sun");
        sun = sunGo.AddComponent<Light>();
        sun.type = LightType.Directional;
        sun.shadows = LightShadows.Soft;
        sunGo.transform.rotation = Quaternion.Euler(48f, -35f, 0f);
        RenderSettings.sun = sun;
        QualitySettings.shadowDistance = 180f;

        LoadTrack(selected, false);
    }

    // ---------- Level / race flow ----------

    void ApplyEnvironment(TrackDef d)
    {
        mainCam.backgroundColor = d.sky;
        RenderSettings.fog = true;
        RenderSettings.fogMode = FogMode.Linear;
        RenderSettings.fogColor = d.sky;
        RenderSettings.fogStartDistance = 220f;
        RenderSettings.fogEndDistance = 1400f;
        RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
        RenderSettings.ambientLight = d.ambient;
        sun.color = d.sunColor;
        sun.intensity = d.sunIntensity;
    }

    void LoadTrack(int index, bool withCar)
    {
        if (track != null) Destroy(track.root);
        if (car != null) { Destroy(car.gameObject); car = null; }

        TrackDef def = TrackLibrary.All[index];
        track = TrackBuilder.Build(def, Gates);
        ApplyEnvironment(def);

        if (withCar)
        {
            int i = track.spawnIdx;
            car = CarController.Create(track.path.pos[i] + Vector3.up * 0.4f,
                Quaternion.LookRotation(track.path.tan[i], Vector3.up), new Color(0.9f, 0.15f, 0.12f));
            follow.target = car.transform;
            follow.targetRb = car.rb;
            follow.orbit = false;
            follow.Snap();
        }
        else
        {
            follow.target = null;
            follow.orbit = true;
            Bounds b = track.bounds;
            follow.orbitCenter = b.center;
            follow.orbitRadius = Mathf.Max(b.size.x, b.size.z) * 0.7f + 30f;
            follow.orbitHeight = follow.orbitRadius * 0.55f;
        }
    }

    void StartRace()
    {
        Time.timeScale = 1f;
        AudioListener.pause = false;
        LoadTrack(selected, true);
        lapTimes.Clear();
        raceTime = 0f; lapStart = 0f; lap = 1; nextGate = 1; flipT = 0f; toastT = 0f;
        respawnIdx = track.spawnIdx;
        prevSd = float.NaN;
        countdownT = 3f;
        state = State.Countdown;
    }

    void ToMenu()
    {
        Time.timeScale = 1f;
        AudioListener.pause = false;
        state = State.Menu;
        LoadTrack(selected, false);
    }

    void SetPause(bool p)
    {
        Time.timeScale = p ? 0f : 1f;
        AudioListener.pause = p;
        state = p ? State.Paused : State.Racing;
    }

    void Respawn()
    {
        int i = respawnIdx;
        car.PlaceAt(track.path.pos[i] + Vector3.up * 0.4f, Quaternion.LookRotation(track.path.tan[i], Vector3.up));
        follow.Snap();
        prevSd = float.NaN;
        flipT = 0f;
    }

    void Update()
    {
        float udt = Time.unscaledDeltaTime;
        if (toastT > 0f) toastT -= udt;
        if (goFlash > 0f) goFlash -= udt;

        switch (state)
        {
            case State.Countdown:
                countdownT -= Time.deltaTime;
                if (countdownT <= 0f) { state = State.Racing; car.controllable = true; goFlash = 0.9f; }
                break;

            case State.Racing:
                raceTime += Time.deltaTime;
                CheckGates();
                if (GameInput.ResetPressed) Respawn();
                if (car.transform.position.y < -30f) Respawn();
                flipT = Vector3.Dot(car.transform.up, Vector3.up) < 0.1f ? flipT + Time.deltaTime : 0f;
                if (flipT > 2.5f) Respawn();
                if (GameInput.PausePressed) SetPause(true);
                break;

            case State.Paused:
                if (GameInput.PausePressed) SetPause(false);
                break;
        }
    }

    void CheckGates()
    {
        TrackPath p = track.path;
        int gi = track.gateIdx[nextGate];
        Vector3 rel = car.transform.position - p.pos[gi];
        float sd = Vector3.Dot(rel, p.tan[gi]);
        bool crossed = !float.IsNaN(prevSd) && prevSd < 0f && sd >= 0f
            && Mathf.Abs(Vector3.Dot(rel, p.right[gi])) < track.def.width * 0.9f
            && Mathf.Abs(rel.y) < 12f;
        if (crossed) { prevSd = float.NaN; PassGate(); }
        else prevSd = sd;
    }

    void PassGate()
    {
        respawnIdx = track.gateIdx[nextGate];
        if (nextGate == 0)
        {
            float lt = raceTime - lapStart;
            lapTimes.Add(lt);
            lapStart = raceTime;
            if (lap >= laps) { Finish(); return; }
            lap++;
            nextGate = 1;
            toast = "LAP " + lap;
        }
        else
        {
            nextGate = (nextGate + 1) % Gates;
            toast = "CHECKPOINT";
        }
        toastT = 1.2f;
    }

    void Finish()
    {
        state = State.Finished;
        car.controllable = false;
        finalTime = raceTime;
        bestLapRace = float.MaxValue;
        foreach (float t in lapTimes) if (t < bestLapRace) bestLapRace = t;
        string kl = "ct.lap." + track.def.id, kt = "ct.total." + track.def.id + "." + laps;
        float pl = PlayerPrefs.GetFloat(kl, 0f), pt = PlayerPrefs.GetFloat(kt, 0f);
        recLap = pl <= 0f || bestLapRace < pl;
        recTotal = pt <= 0f || finalTime < pt;
        if (recLap) PlayerPrefs.SetFloat(kl, bestLapRace);
        if (recTotal) PlayerPrefs.SetFloat(kt, finalTime);
        PlayerPrefs.Save();
    }

    // ---------- UI ----------

    static string Fmt(float t)
    {
        if (t <= 0f || t > 99999f) return "--:--.---";
        int m = (int)(t / 60f);
        float s = t - m * 60f;
        return m + ":" + s.ToString("00.000", CultureInfo.InvariantCulture);
    }

    void EnsureStyles()
    {
        if (sLabel != null) return;
        sLabel = new GUIStyle(GUI.skin.label);
        sLabel.fontStyle = FontStyle.Bold;
        sButton = new GUIStyle(GUI.skin.button);
        sButton.fontSize = 24; sButton.fontStyle = FontStyle.Bold;
        sBig = new GUIStyle(sButton);
        sBig.fontSize = 36;
        sCard = new GUIStyle(sButton);
        sCard.fontSize = 20; sCard.wordWrap = true; sCard.alignment = TextAnchor.MiddleCenter;
        sCard.normal.textColor = Color.white; sCard.hover.textColor = Color.white;
    }

    void Txt(Rect r, string s, int size, TextAnchor a, Color c)
    {
        sLabel.fontSize = size;
        sLabel.alignment = a;
        sLabel.normal.textColor = new Color(0f, 0f, 0f, 0.6f * c.a);
        GUI.Label(new Rect(r.x + 2, r.y + 2, r.width, r.height), s, sLabel);
        sLabel.normal.textColor = c;
        GUI.Label(r, s, sLabel);
    }

    void Dim(float W, float H)
    {
        GUI.color = new Color(0f, 0f, 0f, 0.6f);
        GUI.DrawTexture(new Rect(0, 0, W, H), Texture2D.whiteTexture);
        GUI.color = Color.white;
    }

    void OnGUI()
    {
        EnsureStyles();
        float s = Screen.height / 720f;
        GUI.matrix = Matrix4x4.Scale(new Vector3(s, s, 1f));
        float W = Screen.width / s, H = 720f;

        if (state == State.Menu) { DrawMenu(W, H); return; }
        DrawHud(W, H);
        if (state == State.Paused) DrawPause(W, H);
        if (state == State.Finished) DrawFinish(W, H);
    }

    void DrawMenu(float W, float H)
    {
        Txt(new Rect(0, 40, W, 90), "CHRONOTRACK", 76, TextAnchor.MiddleCenter, Color.white);
        Txt(new Rect(0, 125, W, 40), "Pick a track. Beat the clock.", 26, TextAnchor.MiddleCenter, new Color(1, 1, 1, 0.9f));

        float cw = 340f, gap = 30f;
        float x0 = (W - (cw * 3f + gap * 2f)) * 0.5f;
        for (int i = 0; i < TrackLibrary.All.Length; i++)
        {
            TrackDef d = TrackLibrary.All[i];
            string label = d.name + "\n\n" + d.blurb + "\n\nBest lap: " + Fmt(PlayerPrefs.GetFloat("ct.lap." + d.id, 0f));
            GUI.color = i == selected ? new Color(1f, 0.85f, 0.35f) : new Color(1f, 1f, 1f, 0.85f);
            if (GUI.Button(new Rect(x0 + i * (cw + gap), 220, cw, 210), label, sCard) && i != selected)
            {
                selected = i;
                LoadTrack(i, false);
            }
        }
        GUI.color = Color.white;

        if (GUI.Button(new Rect(W * 0.5f - 110, 455, 220, 46), "Laps: " + laps, sButton))
            laps = laps == 1 ? 3 : (laps == 3 ? 5 : 1);
        if (GUI.Button(new Rect(W * 0.5f - 170, 520, 340, 76), "START", sBig)) StartRace();

        Txt(new Rect(0, H - 70, W, 30), "W/S or Up/Down: throttle & brake     A/D or Left/Right: steer     SPACE: handbrake",
            20, TextAnchor.MiddleCenter, new Color(1, 1, 1, 0.9f));
        Txt(new Rect(0, H - 42, W, 30), "R: back to last checkpoint     ESC: pause", 20, TextAnchor.MiddleCenter, new Color(1, 1, 1, 0.9f));

        if (GUI.Button(new Rect(W - 140, 20, 120, 40), "Quit", sButton)) QuitGame();
    }

    void DrawHud(float W, float H)
    {
        float cur = state == State.Countdown ? 0f : (state == State.Finished ? finalTime : raceTime);
        Txt(new Rect(0, 14, W, 60), Fmt(cur), 52, TextAnchor.MiddleCenter, Color.white);
        float best = PlayerPrefs.GetFloat("ct.lap." + track.def.id, 0f);
        Txt(new Rect(0, 70, W, 30), "LAP " + Mathf.Min(lap, laps) + " / " + laps + "      BEST LAP " + Fmt(best),
            22, TextAnchor.MiddleCenter, new Color(1, 1, 1, 0.9f));

        for (int i = 0; i < lapTimes.Count; i++)
            Txt(new Rect(30, 20 + i * 30, 320, 30), "Lap " + (i + 1) + "   " + Fmt(lapTimes[i]), 22, TextAnchor.MiddleLeft, Color.white);

        int kmh = car != null ? Mathf.RoundToInt(car.SpeedKmh) : 0;
        Txt(new Rect(W - 330, H - 135, 300, 90), kmh.ToString(), 80, TextAnchor.MiddleRight, Color.white);
        Txt(new Rect(W - 330, H - 62, 300, 40), "km/h", 24, TextAnchor.MiddleRight, new Color(1, 1, 1, 0.9f));
        Txt(new Rect(30, H - 50, 500, 30), "R: reset to checkpoint     ESC: pause", 20, TextAnchor.MiddleLeft, new Color(1, 1, 1, 0.8f));

        if (toastT > 0f)
            Txt(new Rect(0, 125, W, 50), toast, 34, TextAnchor.MiddleCenter, new Color(1f, 0.9f, 0.3f, Mathf.Clamp01(toastT * 2f)));

        if (state == State.Countdown)
        {
            int n = Mathf.CeilToInt(countdownT);
            if (n > 0) Txt(new Rect(0, 200, W, 200), n.ToString(), 160, TextAnchor.MiddleCenter, Color.white);
        }
        if (goFlash > 0f)
            Txt(new Rect(0, 200, W, 200), "GO!", 150, TextAnchor.MiddleCenter, new Color(0.4f, 1f, 0.4f, Mathf.Clamp01(goFlash * 2f)));
    }

    void DrawPause(float W, float H)
    {
        Dim(W, H);
        Txt(new Rect(0, 150, W, 80), "PAUSED", 64, TextAnchor.MiddleCenter, Color.white);
        if (GUI.Button(new Rect(W * 0.5f - 150, 270, 300, 60), "Resume", sButton)) SetPause(false);
        if (GUI.Button(new Rect(W * 0.5f - 150, 345, 300, 60), "Restart", sButton)) StartRace();
        if (GUI.Button(new Rect(W * 0.5f - 150, 420, 300, 60), "Main menu", sButton)) ToMenu();
    }

    void DrawFinish(float W, float H)
    {
        Dim(W, H);
        Txt(new Rect(0, 90, W, 80), "FINISHED!", 64, TextAnchor.MiddleCenter, new Color(1f, 0.9f, 0.3f));
        Txt(new Rect(0, 175, W, 60), "Total  " + Fmt(finalTime) + (recTotal ? "   NEW RECORD" : ""), 40, TextAnchor.MiddleCenter, Color.white);
        Txt(new Rect(0, 230, W, 50), "Best lap  " + Fmt(bestLapRace) + (recLap ? "   NEW RECORD" : ""), 30, TextAnchor.MiddleCenter, Color.white);
        for (int i = 0; i < lapTimes.Count; i++)
            Txt(new Rect(0, 290 + i * 30, W, 30), "Lap " + (i + 1) + "   " + Fmt(lapTimes[i]), 22, TextAnchor.MiddleCenter, new Color(1, 1, 1, 0.9f));
        if (GUI.Button(new Rect(W * 0.5f - 320, 520, 300, 66), "Retry", sButton)) StartRace();
        if (GUI.Button(new Rect(W * 0.5f + 20, 520, 300, 66), "Main menu", sButton)) ToMenu();
    }

    void QuitGame()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}
