using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Linq.Expressions;
using Fusion.XR.Shared.Rig;

namespace WebXR.FusionBridge
{
    public class WebXRFusionBridge : MonoBehaviour
    {
        private HardwareRig rig;
        private HardwareHand leftHand;
        private HardwareHand rightHand;

        private MonoBehaviour leftPoseDriver;
        private MonoBehaviour rightPoseDriver;
        private MonoBehaviour leftXRDevice;
        private MonoBehaviour rightPoseDevice;

        private bool webXRActive = false;

        // Reflection caching for WebXR types to bypass assembly definition restrictions
        private static Type managerType;
        private static PropertyInfo pr_Instance;
        private static PropertyInfo pr_XRState;

        private static Type controllerDataType;
        private static FieldInfo fd_hand;
        private static FieldInfo fd_enabled;
        private static FieldInfo fd_gripPosition;
        private static FieldInfo fd_gripRotation;
        private static FieldInfo fd_trigger;
        private static FieldInfo fd_squeeze;
        private static FieldInfo fd_thumbstickTouched;
        private static FieldInfo fd_buttonATouched;
        private static FieldInfo fd_buttonBTouched;
        private static FieldInfo fd_triggerTouched;

        private static Type handDataType;
        private static FieldInfo fd_handEnabled;
        private static FieldInfo fd_handHand;
        private static FieldInfo fd_handTrigger;
        private static FieldInfo fd_handSqueeze;
        private static FieldInfo fd_handJoints;

        private static Type jointDataType;
        private static FieldInfo fd_jointPosition;
        private static FieldInfo fd_jointRotation;

        private static bool reflectionInitialized = false;

        private object onControllerUpdateDelegate;
        private object onHandUpdateDelegate;
        private object onXRChangeDelegate;

        private static void InitializeReflection()
        {
            if (reflectionInitialized) return;

            try
            {
                managerType = Type.GetType("WebXR.WebXRManager, WebXR");
                if (managerType != null)
                {
                    pr_Instance = managerType.GetProperty("Instance", BindingFlags.Public | BindingFlags.Static);
                    pr_XRState = managerType.GetProperty("XRState", BindingFlags.Public | BindingFlags.Instance);
                }

                controllerDataType = Type.GetType("WebXR.WebXRControllerData, WebXR");
                if (controllerDataType != null)
                {
                    fd_hand = controllerDataType.GetField("hand");
                    fd_enabled = controllerDataType.GetField("enabled");
                    fd_gripPosition = controllerDataType.GetField("gripPosition");
                    fd_gripRotation = controllerDataType.GetField("gripRotation");
                    fd_trigger = controllerDataType.GetField("trigger");
                    fd_squeeze = controllerDataType.GetField("squeeze");
                    fd_thumbstickTouched = controllerDataType.GetField("thumbstickTouched");
                    fd_buttonATouched = controllerDataType.GetField("buttonATouched");
                    fd_buttonBTouched = controllerDataType.GetField("buttonBTouched");
                    fd_triggerTouched = controllerDataType.GetField("triggerTouched");
                }

                handDataType = Type.GetType("WebXR.WebXRHandData, WebXR");
                if (handDataType != null)
                {
                    fd_handEnabled = handDataType.GetField("enabled");
                    fd_handHand = handDataType.GetField("hand");
                    fd_handTrigger = handDataType.GetField("trigger");
                    fd_handSqueeze = handDataType.GetField("squeeze");
                    fd_handJoints = handDataType.GetField("joints");
                }

                jointDataType = Type.GetType("WebXR.WebXRJointData, WebXR");
                if (jointDataType != null)
                {
                    fd_jointPosition = jointDataType.GetField("position");
                    fd_jointRotation = jointDataType.GetField("rotation");
                }

                reflectionInitialized = true;
                Debug.Log("[WebXRFusionBridge] Reflection initialized successfully.");
            }
            catch (Exception ex)
            {
                Debug.LogError("[WebXRFusionBridge] Reflection initialization failed: " + ex);
            }
        }

        private bool IsWebXRActiveVR()
        {
            if (managerType == null) return false;
            try
            {
                var instance = pr_Instance.GetValue(null);
                if (instance == null) return false;
                var state = pr_XRState.GetValue(instance);
                return state != null && state.ToString() == "VR";
            }
            catch
            {
                return false;
            }
        }

        private IEnumerator Start()
        {
            InitializeReflection();

            rig = GetComponent<HardwareRig>();
            if (rig == null) yield break;

            leftHand = rig.leftHand;
            rightHand = rig.rightHand;

            if (leftHand != null)
            {
                leftPoseDriver = leftHand.GetComponent("TrackedPoseDriver") as MonoBehaviour;
                leftXRDevice = leftHand.GetComponent("XRControllerInputDevice") as MonoBehaviour;
            }

            if (rightHand != null)
            {
                rightPoseDriver = rightHand.GetComponent("TrackedPoseDriver") as MonoBehaviour;
                rightPoseDevice = rightHand.GetComponent("XRControllerInputDevice") as MonoBehaviour;
            }

            // Wait for WebXRManager to initialize
            while (managerType == null || pr_Instance.GetValue(null) == null)
            {
                yield return null;
            }

            SubscribeToWebXREvents();
            UpdateBridgeState();
        }

        private void OnEnable()
        {
            if (reflectionInitialized)
            {
                SubscribeToWebXREvents();
            }
        }

        private void OnDisable()
        {
            UnsubscribeFromWebXREvents();
        }

        private void OnDestroy()
        {
            UnsubscribeFromWebXREvents();
        }

        private void SubscribeToWebXREvents()
        {
            if (managerType == null) return;

            try
            {
                if (onControllerUpdateDelegate == null)
                    SubscribeStaticEvent(managerType, "OnControllerUpdate", OnControllerUpdateReflection, out onControllerUpdateDelegate);
                if (onHandUpdateDelegate == null)
                    SubscribeStaticEvent(managerType, "OnHandUpdate", OnHandUpdateReflection, out onHandUpdateDelegate);
                if (onXRChangeDelegate == null)
                    SubscribeStaticEvent(managerType, "OnXRChange", OnXRChangeReflection, out onXRChangeDelegate);
            }
            catch (Exception ex)
            {
                Debug.LogError("[WebXRFusionBridge] Error subscribing to WebXR events: " + ex);
            }
        }

        private void UnsubscribeFromWebXREvents()
        {
            if (managerType == null) return;

            try
            {
                if (onControllerUpdateDelegate != null)
                {
                    UnsubscribeStaticEvent(managerType, "OnControllerUpdate", onControllerUpdateDelegate);
                    onControllerUpdateDelegate = null;
                }
                if (onHandUpdateDelegate != null)
                {
                    UnsubscribeStaticEvent(managerType, "OnHandUpdate", onHandUpdateDelegate);
                    onHandUpdateDelegate = null;
                }
                if (onXRChangeDelegate != null)
                {
                    UnsubscribeStaticEvent(managerType, "OnXRChange", onXRChangeDelegate);
                    onXRChangeDelegate = null;
                }
            }
            catch (Exception ex)
            {
                Debug.LogError("[WebXRFusionBridge] Error unsubscribing from WebXR events: " + ex);
            }
        }

        private void OnControllerUpdateReflection(object[] args)
        {
            if (args == null || args.Length < 1) return;
            OnControllerUpdate(args[0]);
        }

        private void OnHandUpdateReflection(object[] args)
        {
            if (args == null || args.Length < 1) return;
            OnHandUpdate(args[0]);
        }

        private void OnXRChangeReflection(object[] args)
        {
            UpdateBridgeState();
        }

        private void UpdateBridgeState()
        {
            webXRActive = IsWebXRActiveVR();
            Debug.Log($"[WebXRFusionBridge] WebXR State Changed. Active VR: {webXRActive}");

            if (webXRActive)
            {
                // Disable components that conflict with WebXR tracking in WebGL VR
                if (leftPoseDriver != null) leftPoseDriver.enabled = false;
                if (rightPoseDriver != null) rightPoseDriver.enabled = false;
                if (leftXRDevice != null) leftXRDevice.enabled = false;
                if (rightPoseDevice != null) rightPoseDevice.enabled = false;

                // Disable regular input actions on HardwareHand so our manual inputs are not overwritten
                if (leftHand != null)
                {
                    leftHand.updateHandCommandWithAction = false;
                    leftHand.updateGrabWithAction = false;
                }
                if (rightHand != null)
                {
                    rightHand.updateHandCommandWithAction = false;
                    rightHand.updateGrabWithAction = false;
                }
            }
            else
            {
                // Restore original tracking components when not in WebXR VR mode
                if (leftPoseDriver != null) leftPoseDriver.enabled = true;
                if (rightPoseDriver != null) rightPoseDriver.enabled = true;
                if (leftXRDevice != null) leftXRDevice.enabled = true;
                if (rightPoseDevice != null) rightPoseDevice.enabled = true;

                if (leftHand != null)
                {
                    leftHand.updateHandCommandWithAction = true;
                    leftHand.updateGrabWithAction = true;
                }
                if (rightHand != null)
                {
                    rightHand.updateHandCommandWithAction = true;
                    rightHand.updateGrabWithAction = true;
                }
            }
        }

        private void OnControllerUpdate(object data)
        {
            if (!webXRActive || data == null) return;

            bool isEnabled = (bool)fd_enabled.GetValue(data);
            if (!isEnabled) return;

            int handVal = (int)fd_hand.GetValue(data);
            if (handVal == 1 && leftHand != null) // 1 = LEFT
            {
                UpdateHandTransformAndInput(leftHand, data);
            }
            else if (handVal == 2 && rightHand != null) // 2 = RIGHT
            {
                UpdateHandTransformAndInput(rightHand, data);
            }
        }

        private void OnHandUpdate(object data)
        {
            if (!webXRActive || data == null) return;

            bool isEnabled = (bool)fd_handEnabled.GetValue(data);
            if (!isEnabled) return;

            int handVal = (int)fd_handHand.GetValue(data);
            if (handVal == 1 && leftHand != null) // 1 = LEFT
            {
                UpdateHandDataTransformAndInput(leftHand, data);
            }
            else if (handVal == 2 && rightHand != null) // 2 = RIGHT
            {
                UpdateHandDataTransformAndInput(rightHand, data);
            }
        }

        private void UpdateHandTransformAndInput(HardwareHand hand, object data)
        {
            // Apply position and rotation directly
            hand.transform.localPosition = (Vector3)fd_gripPosition.GetValue(data);
            hand.transform.localRotation = (Quaternion)fd_gripRotation.GetValue(data);

            // Sync hand poses / gestures for Photon Fusion
            float trigger = (float)fd_trigger.GetValue(data);
            float squeeze = (float)fd_squeeze.GetValue(data);
            bool thumbstickTouched = (bool)fd_thumbstickTouched.GetValue(data);
            bool buttonATouched = (bool)fd_buttonATouched.GetValue(data);
            bool buttonBTouched = (bool)fd_buttonBTouched.GetValue(data);
            bool triggerTouched = (bool)fd_triggerTouched.GetValue(data);

            hand.handCommand.triggerCommand = trigger;
            hand.handCommand.gripCommand = squeeze;
            hand.handCommand.thumbTouchedCommand = (thumbstickTouched || buttonATouched || buttonBTouched) ? 1f : 0f;
            hand.handCommand.indexTouchedCommand = triggerTouched ? 1f : 0f;
            hand.isGrabbing = squeeze > hand.grabThreshold;
        }

        private void UpdateHandDataTransformAndInput(HardwareHand hand, object data)
        {
            // Hand tracking (joints) updates
            Array joints = fd_handJoints.GetValue(data) as Array;
            if (joints != null && joints.Length > 0)
            {
                object wristJoint = joints.GetValue(0); // wrist/root joint
                Vector3 position = (Vector3)fd_jointPosition.GetValue(wristJoint);
                Quaternion rotation = (Quaternion)fd_jointRotation.GetValue(wristJoint);

                hand.transform.localPosition = position;
                hand.transform.localRotation = rotation;
            }

            // Sync inputs from hand data
            float trigger = (float)fd_handTrigger.GetValue(data);
            float squeeze = (float)fd_handSqueeze.GetValue(data);

            hand.handCommand.triggerCommand = trigger;
            hand.handCommand.gripCommand = squeeze;
            hand.isGrabbing = squeeze > hand.grabThreshold;
        }

        private static void SubscribeStaticEvent(Type type, string eventName, Action<object[]> handler, out object delegateInstance)
        {
            delegateInstance = null;
            var eventInfo = type.GetEvent(eventName, BindingFlags.Public | BindingFlags.Static);
            if (eventInfo == null) return;

            var delegateType = eventInfo.EventHandlerType;
            var invokeMethod = delegateType.GetMethod("Invoke");
            var parameterTypes = new List<Type>();
            foreach (var p in invokeMethod.GetParameters())
            {
                parameterTypes.Add(p.ParameterType);
            }

            var parameters = new ParameterExpression[parameterTypes.Count];
            for (int i = 0; i < parameterTypes.Count; i++)
            {
                parameters[i] = Expression.Parameter(parameterTypes[i], "p" + i);
            }

            var handlerValue = Expression.Constant(handler);
            var castParams = new Expression[parameterTypes.Count];
            for (int i = 0; i < parameterTypes.Count; i++)
            {
                castParams[i] = Expression.Convert(parameters[i], typeof(object));
            }

            var arrayExpr = Expression.NewArrayInit(typeof(object), castParams);
            var call = Expression.Invoke(handlerValue, arrayExpr);

            var lambda = Expression.Lambda(delegateType, call, parameters);
            delegateInstance = lambda.Compile();

            eventInfo.AddMethod.Invoke(null, new object[] { delegateInstance });
        }

        private static void UnsubscribeStaticEvent(Type type, string eventName, object delegateInstance)
        {
            if (delegateInstance == null) return;
            var eventInfo = type.GetEvent(eventName, BindingFlags.Public | BindingFlags.Static);
            if (eventInfo == null) return;

            eventInfo.RemoveMethod.Invoke(null, new object[] { delegateInstance });
        }
    }

    public class WebXRFusionBridgeLoader : MonoBehaviour
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Initialize()
        {
            InitializeReflectionStatic();
            var go = new GameObject("WebXRFusionBridgeLoader");
            go.AddComponent<WebXRFusionBridgeLoader>();
            DontDestroyOnLoad(go);
            Debug.Log("[WebXRFusionBridgeLoader] Global dynamic loader initialized.");
        }

        private static void InitializeReflectionStatic()
        {
            try
            {
                // Force early reflection initialization
                var initMethod = typeof(WebXRFusionBridge).GetMethod("InitializeReflection", BindingFlags.NonPublic | BindingFlags.Static);
                if (initMethod != null)
                {
                    initMethod.Invoke(null, null);
                }
            }
            catch { }
        }

        private void Update()
        {
            // Scan for any HardwareRigs that do not have the bridge component yet
            var rigs = FindObjectsByType<HardwareRig>(FindObjectsSortMode.None);
            foreach (var rig in rigs)
            {
                if (rig.GetComponent<WebXRFusionBridge>() == null)
                {
                    rig.gameObject.AddComponent<WebXRFusionBridge>();
                    Debug.Log($"[WebXRFusionBridgeLoader] Found and dynamically attached WebXRFusionBridge to HardwareRig: {rig.name}");
                }
            }
        }
    }
}