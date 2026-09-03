using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(GameSettings))]
public class GameSettingsEditor : Editor
{
    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        DrawScreenSettings();
        DrawGameModeSettings();
        DrawRecordingSettings();

        SerializedProperty gameModeProp = serializedObject.FindProperty("gameMode");
        GameMode currentMode = (GameMode)gameModeProp.enumValueIndex;

        if (currentMode == GameMode.DuelBreakout)
        {
            DrawDuelBreakoutSettings();
        }
        else
        {
            DrawStandardModeSettings();
        }

        EditorGUILayout.Space(10);
        if (GUILayout.Button("Reset To Recommended Values"))
        {
            foreach (Object targetObj in targets)
            {
                GameSettings settings = targetObj as GameSettings;
                if (settings == null)
                {
                    continue;
                }

                Undo.RecordObject(settings, "Reset GameSettings");
                settings.ResetToRecommended();
                EditorUtility.SetDirty(settings);
            }
        }

        serializedObject.ApplyModifiedProperties();
    }

    private void DrawScreenSettings()
    {
        DrawSectionHeader("Screen Settings");
        DrawProperty("cameraOrthoSize");
        DrawProperty("forceResolution");
    }

    private void DrawGameModeSettings()
    {
        DrawSectionHeader("Game Mode");
        DrawProperty("gameMode");
    }

    private void DrawRecordingSettings()
    {
        DrawSectionHeader("Recording");
        DrawProperty("recordingMode");
        DrawProperty("autoStartRecording");
        DrawProperty("batchDuration");
        DrawProperty("minRecordingLength");
        DrawProperty("maxRecordingLength");
        DrawProperty("enableParameterRandomization");
    }

    private void DrawStandardModeSettings()
    {
        DrawSectionHeader("Shape Type");
        DrawProperty("shapeType");

        DrawSectionHeader("Ring Settings");
        DrawProperty("ringRadius");
        DrawProperty("ringThickness");
        DrawProperty("gapAngleDegrees");
        DrawProperty("gapInitialAngle");
        DrawProperty("rotationSpeed");
        DrawProperty("ringColor");
        DrawProperty("shapeColorMinBrightness");
        DrawProperty("shapeColorMinSaturation");
        DrawProperty("ringVerticalOffset");

        DrawSectionHeader("Ellipse Settings");
        DrawProperty("ellipseWidthRadius");
        DrawProperty("ellipseHeightRadius");

        DrawSectionHeader("Ball Settings");
        DrawProperty("ballRadius");
        DrawProperty("bounciness");
        DrawProperty("friction");
        DrawProperty("ballColorMinBrightness");
        DrawProperty("ballColorMinSaturation");

        DrawSectionHeader("Ball Freeze & Spawn");
        DrawProperty("freezeMode");
        DrawProperty("ballFreezeTime");
        DrawProperty("ballMaxBounces");
        DrawProperty("enableFreezeIncrementation");
        DrawProperty("freezeIncrementStep");
        DrawProperty("showCounter");
        DrawProperty("enableInstantFreezeOnShapeContact");
        DrawProperty("enableFreezeEffect");
        DrawProperty("enableFrozenBallRotation");
        DrawProperty("freezeCollisionClip");
        DrawProperty("freezeCollisionVolume");

        DrawSectionHeader("Physics");
        DrawProperty("gravity");

        DrawSectionHeader("Game Loop");
        DrawProperty("gameOverAnimationDuration");

        DrawSectionHeader("Spawn Settings");
        DrawProperty("fixedSpawnPosition");
        DrawProperty("spawnAngleMin");
        DrawProperty("spawnAngleMax");
        DrawProperty("spawnRadiusPercent");

        DrawSectionHeader("Escape Detection");
        DrawProperty("escapeBuffer");

        DrawSectionHeader("Trail Effect");
        DrawProperty("trailStyle");
        DrawProperty("trailTime");
        DrawProperty("trailWidthMultiplier");

        DrawSectionHeader("Game Over Effects");
        DrawProperty("ringParticleCount");
    }

    private void DrawDuelBreakoutSettings()
    {
        DrawSectionHeader("Duel Breakout Settings");
        DrawProperty("ballClass1");
        DrawProperty("ballClass2");
        DrawProperty("duelRingCount");
        DrawProperty("duelRings", true);
    }

    private void DrawSectionHeader(string title)
    {
        EditorGUILayout.Space(8);
        EditorGUILayout.LabelField(title, EditorStyles.boldLabel);
    }

    private void DrawProperty(string propertyName, bool includeChildren = false)
    {
        SerializedProperty prop = serializedObject.FindProperty(propertyName);
        if (prop != null)
        {
            EditorGUILayout.PropertyField(prop, includeChildren);
        }
    }
}
