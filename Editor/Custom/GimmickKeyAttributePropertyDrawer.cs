using System;
using System.Collections.Generic;
using System.Linq;
using ClusterVR.CreatorKit.Gimmick;
using ClusterVR.CreatorKit.Gimmick.Implements;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine.UIElements;

namespace ClusterVR.CreatorKit.Editor.Custom
{
    [CustomPropertyDrawer(typeof(GimmickKeyAttribute), true)]
    public class GimmickKeyAttributePropertyDrawer : PropertyDrawer
    {
        public override VisualElement CreatePropertyGUI(SerializedProperty property)
        {
            var targetChoices = attribute is GimmickKeyAttribute keyAttr ?
                keyAttr.TargetSelectables :
                Enum.GetValues(typeof(Target)).Cast<Target>();
            return CreatePropertyGUI(property, targetChoices.ToList());
        }

        static VisualElement CreatePropertyGUI(SerializedProperty property, List<Target> targetChoices)
        {
            var container = new VisualElement();

            var targetProperty = property.FindPropertyRelative("target");
            var targetField = new PopupField<Target>("Target", targetChoices, (Target)targetProperty.enumValueIndex, FormatListItem, FormatListItem);
            targetField.RegisterValueChangedCallback(e =>
            {
                targetProperty.enumValueIndex = (int) e.newValue;
                property.serializedObject.ApplyModifiedProperties();
            });

            var keyField = new PropertyField(property.FindPropertyRelative("key"));

            container.Add(targetField);
            container.Add(keyField);

            return container;
        }

        static string FormatListItem(Target target)
        {
            switch (target)
            {
                case Target.Item:
                    return "This";
                default:
                    return target.ToString();
            }
        }
    }
}
