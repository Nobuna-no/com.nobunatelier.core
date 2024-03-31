using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

namespace NobunAtelier.Editor
{
    public class AnimSequenceBaker : EditorWindow
    {
        private AnimatorOverrideController overrideController;
        private AnimatorController controller;

        private Dictionary<int, AnimMontageDataToBake> m_animMontagesData = new Dictionary<int, AnimMontageDataToBake>();

        private Vector2 m_scrollPosition = Vector2.zero;

        private AnimSequenceCollection m_animationCollection;

        private bool m_showOnlyValidAnimationClip = true;
        private bool m_useAnimationControllerOverride = false;

        [MenuItem("NobunAtelier/Animation Sequence Baker")]
        public static void ShowWindow()
        {
            GetWindow<AnimSequenceBaker>("Animation Sequence Baker");
        }

        private void OnGUI()
        {
            using (new EditorGUILayout.VerticalScope(GUI.skin.window, GUILayout.Height(EditorGUIUtility.singleLineHeight * 4)))
            {
                using (new EditorGUILayout.HorizontalScope())
                {
                    controller = EditorGUILayout.ObjectField("Animator Controller", controller, typeof(AnimatorController), true) as AnimatorController;
                    if (m_useAnimationControllerOverride)
                    {
                        overrideController = EditorGUILayout.ObjectField("Animator Controller", overrideController, typeof(AnimatorOverrideController), true) as AnimatorOverrideController;
                    }

                    m_useAnimationControllerOverride = GUILayout.Toggle(m_useAnimationControllerOverride, "Use AnimationController", GUILayout.ExpandWidth(false));
                }

                m_animationCollection = EditorGUILayout.ObjectField("Animation Montage Collection", m_animationCollection, typeof(AnimSequenceCollection), false) as AnimSequenceCollection;

                using (new EditorGUILayout.HorizontalScope())
                {
                    m_showOnlyValidAnimationClip = GUILayout.Toggle(m_showOnlyValidAnimationClip, m_showOnlyValidAnimationClip ? "Displays Only Valid" : "Show All", GUI.skin.button, GUILayout.ExpandWidth(false));
                    if (GUILayout.Button("Refresh", GUILayout.ExpandWidth(false)))
                    {
                        m_animMontagesData.Clear();
                        m_scrollPosition = Vector2.zero;
                    }
                }
            }

            if (controller == null)
            {
                if (m_animMontagesData.Count > 0)
                {
                    m_animMontagesData.Clear();
                    m_scrollPosition = Vector2.zero;
                }

                return;
            }

            EditorGUILayout.Space();
            EditorGUILayout.HelpBox($"Add animation event with {typeof(AnimSegmentDefinition).Name} parameter to this animation to generate montage segments." +
                $"You can use 'AnimMontageController' component to provide 'OnAnimationSegmentTrigger' event to your animator.", MessageType.Info);

            EditorGUILayout.LabelField("Animation Clips:", EditorStyles.boldLabel);

            m_scrollPosition = EditorGUILayout.BeginScrollView(m_scrollPosition);

            var animStates = GetAnimatorControllerStateInfo();

            foreach (var state in animStates)
            {
                if (!(state.motion is AnimationClip))
                {
                    continue;
                }

                AnimationClip clip = (AnimationClip)state.motion;

                if (!m_animMontagesData.ContainsKey(state.nameHash))
                {
                    m_animMontagesData.Add(state.nameHash, new AnimMontageDataToBake()
                    {
                        stateNameHash = state.nameHash,
                        stateName = state.name,
                        animSegmentsData = new List<AnimSegmentDataToBake>(),
                        hasAvailableData = false,
                        selectedIndex = 0
                    });

                    var newAnimData = m_animMontagesData[state.nameHash];
                    GetClipEventNames(clip, out newAnimData.availableSegments);

                    if (newAnimData.availableSegments.Count > 0)
                    {
                        newAnimData.hasAvailableData = true;

                        foreach (var segment in newAnimData.availableSegments)
                        {
                            newAnimData.animSegmentsData.Add(new AnimSegmentDataToBake()
                            {
                                definition = segment
                            });
                        }

                        newAnimData.availableSegments.Clear();
                        RefreshAnimSegmentsData(clip, newAnimData);
                    }
                }

                var currentAnimData = m_animMontagesData[state.nameHash];
                if (m_showOnlyValidAnimationClip && !currentAnimData.hasAvailableData)
                {
                    continue;
                }

                using (new EditorGUILayout.VerticalScope(GUI.skin.box))
                {
                    currentAnimData.foldout = EditorGUILayout.Foldout(currentAnimData.foldout, $"{state.name} ({clip.length.ToString("F2")} seconds)");
                    if (!currentAnimData.foldout)
                    {
                        continue;
                    }

                    bool hasAvailableSegment = currentAnimData.availableSegments.Count > 0;
                    // currentAnimData.availableSegments.Count > 0
                    EditorGUILayout.LabelField(hasAvailableSegment ? "Available Segments:" : "No AnimSegment event found.");

                    if (currentAnimData.availableSegments.Count > 0)
                    {
                        using (new EditorGUILayout.HorizontalScope())
                        {
                            currentAnimData.selectedIndex = Mathf.Clamp(currentAnimData.selectedIndex, 0, currentAnimData.availableSegments.Count - 1);
                            currentAnimData.selectedIndex = EditorGUILayout.Popup(currentAnimData.selectedIndex, currentAnimData.availableSegments.Select(x => x.name).ToArray());
                            if (GUILayout.Button("+", GUILayout.Width(20)))
                            {
                                currentAnimData.animSegmentsData.Add(new AnimSegmentDataToBake()
                                {
                                    definition = currentAnimData.availableSegments[currentAnimData.selectedIndex]
                                });

                                RefreshAvailableSegment(currentAnimData);
                                RefreshAnimSegmentsData(clip, currentAnimData);
                            }
                        }
                    }

                    if (currentAnimData.animSegmentsData.Count > 0)
                    {
                        using (new EditorGUILayout.VerticalScope())
                        {
                            AnimSegmentDataToBake segmentToRemove = null;
                            for (int i = 0, c = currentAnimData.animSegmentsData.Count; i < c; ++i)
                            {
                                var segment = currentAnimData.animSegmentsData[i];
                                using (new EditorGUILayout.HorizontalScope())
                                {
                                    EditorGUILayout.LabelField("* " + segment.definition.name + ": " + segment.duration.ToString("F2") + " seconds");
                                    if (GUILayout.Button("-", GUILayout.Width(20)))
                                    {
                                        segmentToRemove = segment;
                                    }
                                }
                            }

                            if (segmentToRemove != null)
                            {
                                currentAnimData.animSegmentsData.Remove(segmentToRemove);
                                currentAnimData.availableSegments.Add(segmentToRemove.definition);
                                RefreshAvailableSegment(currentAnimData);
                                RefreshAnimSegmentsData(clip, currentAnimData);
                            }
                        }
                    }

                    using (new EditorGUILayout.HorizontalScope())
                    {
                        if (!ExtractAndRefreshAvailableSegment(currentAnimData, clip))
                        {
                            // If no available segment, skip to the next animation.
                            continue;
                        }

                        currentAnimData.saveInCollection = GUILayout.Toggle(currentAnimData.saveInCollection, currentAnimData.saveInCollection ? "Save In Collection" : "Save in Definition", GUI.skin.button);
                    }

                    if (currentAnimData.saveInCollection)
                    {
                        using (new EditorGUI.DisabledGroupScope(m_animationCollection == null))
                        {
                            if (GUILayout.Button($"Add '{state.name}' to Collection"))
                            {
                                var def = m_animationCollection.GetOrCreateDefinition(state.name) as AnimSequenceDefinition;
                                BakeAnimSequenceData(state, clip, currentAnimData, def);
                                EditorUtility.SetDirty(def);
                                AssetDatabase.SaveAssetIfDirty(def);
                                m_animationCollection.SaveCollection();
                                currentAnimData.sequenceDefinitionToUpdate = def;
                                currentAnimData.saveInCollection = false;

                                Selection.activeObject = def;
                                EditorGUIUtility.PingObject(def);
                            }
                        }
                    }
                    else
                    {
                        using (new EditorGUILayout.HorizontalScope())
                        {
                            currentAnimData.sequenceDefinitionToUpdate = EditorGUILayout.ObjectField(currentAnimData.sequenceDefinitionToUpdate, typeof(AnimSequenceDefinition), false) as AnimSequenceDefinition;

                            using (new EditorGUI.DisabledGroupScope(currentAnimData.sequenceDefinitionToUpdate == null))
                            {
                                if (GUILayout.Button("Update Definition"))
                                {
                                    var def = currentAnimData.sequenceDefinitionToUpdate;

                                    BakeAnimSequenceData(state, clip, currentAnimData, def);

                                    EditorUtility.SetDirty(def);
                                    AssetDatabase.SaveAssets();
                                    AssetDatabase.Refresh();

                                    Selection.activeObject = def;
                                    EditorGUIUtility.PingObject(def);
                                }
                            }
                        }
                    }
                    EditorGUILayout.Space();
                }
            }
            EditorGUILayout.EndScrollView();
        }

        private static void BakeAnimSequenceData(AnimatorState state, AnimationClip clip, AnimMontageDataToBake currentAnimData, AnimSequenceDefinition def)
        {
            // def.clip = clip;
            def.stateNameHash = state.nameHash;
            def.stateName = state.name;

            def.segments = new AnimSequenceDefinition.Segment[currentAnimData.animSegmentsData.Count];
            SerializedObject serializedObject = new SerializedObject(def);
            for (int i = currentAnimData.animSegmentsData.Count - 1; i >= 0; --i)
            {
                var segment = currentAnimData.animSegmentsData[i];
                def.segments[i] = new AnimSequenceDefinition.Segment(segment.duration, segment.definition);

                // Total length... deprecated feature...
                // var durationProperty = serializedObject.FindProperty("m_duration");
                // durationProperty.floatValue = segment.duration;
            }
            serializedObject.ApplyModifiedProperties();
        }

        private static void RefreshAnimSegmentsData(AnimationClip clip, AnimMontageDataToBake currentAnimData)
        {
            float lastEventTime = clip.length;

            for (int i = clip.events.Length - 1; i >= 0; i--)
            {
                var animationEvent = clip.events[i];

                for (int j = currentAnimData.animSegmentsData.Count - 1; j >= 0; --j)
                {
                    var currentSegment = currentAnimData.animSegmentsData[j];
                    if (currentSegment.definition == animationEvent.objectReferenceParameter)
                    {
                        float duration = lastEventTime - animationEvent.time;
                        currentSegment.eventTime = animationEvent.time;
                        currentSegment.duration = duration;
                        lastEventTime = animationEvent.time;
                    }
                }
            }

            currentAnimData.animSegmentsData.Sort((x, y) => x.eventTime.CompareTo(y.eventTime));
        }

        private bool ExtractAndRefreshAvailableSegment(AnimMontageDataToBake currentAnimData, AnimationClip clip)
        {
            if (GUILayout.Button("Extract All Available Segments"))
            {
                GetClipEventNames(clip, out currentAnimData.availableSegments);
                if (currentAnimData.availableSegments.Count == 0)
                {
                    currentAnimData.hasAvailableData = false;
                    currentAnimData.animSegmentsData.Clear();
                    return false;
                }
            }
            else
            {
                return currentAnimData.hasAvailableData;
            }

            currentAnimData.hasAvailableData = true;

            RefreshAvailableSegment(currentAnimData);

            foreach (var newSegment in currentAnimData.availableSegments)
            {
                currentAnimData.animSegmentsData.Add(new AnimSegmentDataToBake()
                {
                    definition = newSegment
                });
            }

            RefreshAvailableSegment(currentAnimData);
            RefreshAnimSegmentsData(clip, currentAnimData);

            return true;
        }

        private void RefreshAvailableSegment(AnimMontageDataToBake currentAnimData)
        {
            foreach (var segment in currentAnimData.animSegmentsData)
            {
                if (currentAnimData.availableSegments.Contains(segment.definition))
                {
                    currentAnimData.availableSegments.Remove(segment.definition);
                }
            }
        }

        private void GetClipEventNames(AnimationClip clip, out List<AnimSegmentDefinition> availableSegments)
        {
            availableSegments = new List<AnimSegmentDefinition>();
            HashSet<AnimSegmentDefinition> eventNames = new HashSet<AnimSegmentDefinition>();

            foreach (AnimationEvent animationEvent in clip.events)
            {
                if (animationEvent.objectReferenceParameter != null && animationEvent.objectReferenceParameter is AnimSegmentDefinition)
                {
                    if (eventNames.Contains(animationEvent.objectReferenceParameter))
                    {
                        Debug.LogWarning($"{this.name}: segment '{animationEvent.objectReferenceParameter}' has already been extracted. " +
                            $"This happens when you assigned several time the same AnimationSegmentDefinition on the same animation clip.");
                    }
                    else
                    {
                        eventNames.Add(animationEvent.objectReferenceParameter as AnimSegmentDefinition);
                    }
                }
            }

            availableSegments.AddRange(eventNames);
        }

        public List<AnimatorState> GetAnimatorControllerStateInfo()
        {
            List<AnimatorState> stateHashes = new List<AnimatorState>();
            if (controller == null)
            {
                return stateHashes;
            }


            foreach (AnimatorControllerLayer layer in this.controller.layers)
            {
                foreach (ChildAnimatorState state in layer.stateMachine.states)
                {
                    stateHashes.Add(state.state);
                }

                // Foreach state in sub state machines of the layer
                foreach (ChildAnimatorStateMachine stateMachine in layer.stateMachine.stateMachines)
                {
                    foreach (ChildAnimatorState state in layer.stateMachine.states)
                    {
                        stateHashes.Add(state.state);
                    }
                }
            }

            return stateHashes;
        }

        [System.Serializable]
        public class AnimMontageDataToBake
        {
            public List<AnimSegmentDataToBake> animSegmentsData;
            public AnimSequenceDefinition sequenceDefinitionToUpdate;
            public List<AnimSegmentDefinition> availableSegments;
            public int stateNameHash;
            public string stateName;
            public int selectedIndex;
            public bool foldout = true;
            public bool hasAvailableData = false;
            public bool saveInCollection = true;
        }

        [System.Serializable]
        public class AnimSegmentDataToBake
        {
            public AnimSegmentDefinition definition;
            public float eventTime;
            public float duration;
            public float sequenceTargetDuration;
        }

        private class FunctionAndAnimationSegmentDefinition
        {
            public string name;
            public AnimSegmentDefinition definition;
        }
    }
}
