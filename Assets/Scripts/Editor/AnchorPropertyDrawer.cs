//using UnityEditor;
//using UnityEngine;
//using UnityEngine.UIElements;
//using UnityEditor.UIElements;

//namespace BezierCurveDemo
//{
//    [CustomPropertyDrawer(typeof(Path.Anchor))]
//    public class AnchorPropertyDrawer : PropertyDrawer
//    {
//        const string backTangentPath = "backTangent";
//        const string positionPath = "position";
//        const string frontTangentPath = "frontTangent";
//        const string handleSettingPath = "handleSetting";

//        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
//        {
//            return EditorGUIUtility.singleLineHeight * 4;
//        }

//        public override VisualElement CreatePropertyGUI(SerializedProperty property)
//        {
//            var root = new VisualElement();

//            var backTangentField = new Vector3Field("Back Tangent");
//            backTangentField.bindingPath = backTangentPath;
//            SetUpAssignmentOnChangeEvent(backTangentField, property, (anchor, value) =>
//            {
//                if (anchor.ShouldAlign) { anchor.AlignFrontTangent(); }
//            });
//            root.Add(backTangentField);

//            var positionField = new Vector3Field("Position");
//            positionField.bindingPath = positionPath;
//            root.Add(positionField);

//            var frontTangentField = new Vector3Field("Front Tangent");
//            frontTangentField.bindingPath = frontTangentPath;
//            SetUpAssignmentOnChangeEvent(frontTangentField, property, (anchor, value) =>
//            {
//                if (anchor.ShouldAlign) { anchor.AlignBackTangent(); }
//            });
//            root.Add(frontTangentField);

//            var handleSettingField = new EnumField("Handle Setting", Path.Anchor.HandleType.Aligned);
//            handleSettingField.bindingPath = handleSettingPath;
//            SetUpAssignmentOnChangeEvent(handleSettingField, property, (anchor, value) =>
//            {
//                if (anchor.ShouldAlign) { anchor.AlignTangents(); }
//            });
//            root.Add(handleSettingField);
//            return root;
//        }

//        void SetUpAssignmentOnChangeEvent<T>(INotifyValueChanged<T> field, SerializedProperty property, System.Action<Path.Anchor, T> assignment)
//        {
//            field.RegisterValueChangedCallback((evt) =>
//            {
//                var anchor = GetAnchorFromProperty(property);
//                assignment(anchor, evt.newValue);
//                SetAnchorValuesToProperty(anchor, property);
//                property.serializedObject.ApplyModifiedProperties();
//            });
//        }

//        Path.Anchor GetAnchorFromProperty(SerializedProperty property)
//        {
//            return new Path.Anchor(
//                property.FindPropertyRelative(backTangentPath).vector3Value,
//                property.FindPropertyRelative(positionPath).vector3Value,
//                property.FindPropertyRelative(frontTangentPath).vector3Value,
//                (Path.Anchor.HandleType)property.FindPropertyRelative(handleSettingPath).enumValueIndex);
//        }

//        void SetAnchorValuesToProperty(in Path.Anchor anchor, SerializedProperty property)
//        {
//            property.FindPropertyRelative(backTangentPath).vector3Value = anchor.BackTangent;
//            property.FindPropertyRelative(positionPath).vector3Value = anchor.Position;
//            property.FindPropertyRelative(frontTangentPath).vector3Value = anchor.FrontTangent;
//            property.FindPropertyRelative(handleSettingPath).enumValueIndex = (int)anchor.HandleSetting;
//        }
//    }
//}