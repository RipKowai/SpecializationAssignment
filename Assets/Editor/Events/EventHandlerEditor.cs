using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace Events
{
    [CustomEditor(typeof(EventHandler), true)]
    public class EventHandlerEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();

            EventHandler eh = target as EventHandler;
            GUILayout.Space(10);
            EditorGUILayout.LabelField("Event Stack", EditorStyles.boldLabel);
            GUILayout.BeginVertical(EditorStyles.helpBox);
            foreach (EventHandler.IEvent evt in eh.EventStack)
            {
                string name = "   #" + eh.EventStack.IndexOf(evt) + ": " + evt.ToString();
                if (evt is Object obj)
                {
                    if (GUILayout.Button(name, evt == eh.CurrentEvent ? EditorStyles.boldLabel : EditorStyles.label))
                    {
                        Selection.activeObject = obj;
                    }
                }
                else
                {
                    EditorGUILayout.LabelField(name, evt == eh.CurrentEvent ? EditorStyles.boldLabel : EditorStyles.label);
                }
            }
            GUILayout.EndVertical();
        }

    }
}