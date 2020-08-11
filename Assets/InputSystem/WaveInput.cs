// GENERATED AUTOMATICALLY FROM 'Assets/InputSystem/WaveInput.inputactions'

using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Utilities;

namespace AL.NOISE.InputSystem
{
    public class @WaveInput : IInputActionCollection, IDisposable
    {
        public InputActionAsset asset { get; }
        public @WaveInput()
        {
            asset = InputActionAsset.FromJson(@"{
    ""name"": ""WaveInput"",
    ""maps"": [
        {
            ""name"": ""WaveController"",
            ""id"": ""c7ddc4ea-e789-4ecc-878b-d98f3170e996"",
            ""actions"": [
                {
                    ""name"": ""BeatMultiplier"",
                    ""type"": ""Value"",
                    ""id"": ""4efad281-c968-4bf4-a919-b388974f0790"",
                    ""expectedControlType"": ""Vector2"",
                    ""processors"": """",
                    ""interactions"": """"
                },
                {
                    ""name"": ""BandSelector"",
                    ""type"": ""Button"",
                    ""id"": ""a54fef2f-ebca-4293-acb6-fac92d7e0505"",
                    ""expectedControlType"": ""Button"",
                    ""processors"": """",
                    ""interactions"": """"
                },
                {
                    ""name"": ""ShowHideUi"",
                    ""type"": ""Button"",
                    ""id"": ""eca41a62-f6ee-4e92-93ac-c4ef7440e102"",
                    ""expectedControlType"": ""Button"",
                    ""processors"": """",
                    ""interactions"": """"
                }
            ],
            ""bindings"": [
                {
                    ""name"": """",
                    ""id"": ""5bfc98a6-8619-41e8-ba9d-807ab29ef513"",
                    ""path"": ""<Mouse>/scroll"",
                    ""interactions"": """",
                    ""processors"": ""NormalizeVector2"",
                    ""groups"": """",
                    ""action"": ""BeatMultiplier"",
                    ""isComposite"": false,
                    ""isPartOfComposite"": false
                },
                {
                    ""name"": ""Selector"",
                    ""id"": ""04fa18ec-702d-4d74-b319-b00f2c658bd4"",
                    ""path"": ""1DAxis"",
                    ""interactions"": """",
                    ""processors"": """",
                    ""groups"": """",
                    ""action"": ""BandSelector"",
                    ""isComposite"": true,
                    ""isPartOfComposite"": false
                },
                {
                    ""name"": ""negative"",
                    ""id"": ""65b1cbfc-aeb4-4b52-8f89-1af183df5ffa"",
                    ""path"": ""<Keyboard>/leftArrow"",
                    ""interactions"": """",
                    ""processors"": """",
                    ""groups"": """",
                    ""action"": ""BandSelector"",
                    ""isComposite"": false,
                    ""isPartOfComposite"": true
                },
                {
                    ""name"": ""positive"",
                    ""id"": ""5823e39d-d473-43d0-b111-acec5ef17ddd"",
                    ""path"": ""<Keyboard>/rightArrow"",
                    ""interactions"": """",
                    ""processors"": """",
                    ""groups"": """",
                    ""action"": ""BandSelector"",
                    ""isComposite"": false,
                    ""isPartOfComposite"": true
                },
                {
                    ""name"": """",
                    ""id"": ""09d10424-3ace-4f5c-8565-17de5a501320"",
                    ""path"": ""<Keyboard>/f5"",
                    ""interactions"": """",
                    ""processors"": """",
                    ""groups"": """",
                    ""action"": ""ShowHideUi"",
                    ""isComposite"": false,
                    ""isPartOfComposite"": false
                }
            ]
        }
    ],
    ""controlSchemes"": []
}");
            // WaveController
            m_WaveController = asset.FindActionMap("WaveController", throwIfNotFound: true);
            m_WaveController_BeatMultiplier = m_WaveController.FindAction("BeatMultiplier", throwIfNotFound: true);
            m_WaveController_BandSelector = m_WaveController.FindAction("BandSelector", throwIfNotFound: true);
            m_WaveController_ShowHideUi = m_WaveController.FindAction("ShowHideUi", throwIfNotFound: true);
        }

        public void Dispose()
        {
            UnityEngine.Object.Destroy(asset);
        }

        public InputBinding? bindingMask
        {
            get => asset.bindingMask;
            set => asset.bindingMask = value;
        }

        public ReadOnlyArray<InputDevice>? devices
        {
            get => asset.devices;
            set => asset.devices = value;
        }

        public ReadOnlyArray<InputControlScheme> controlSchemes => asset.controlSchemes;

        public bool Contains(InputAction action)
        {
            return asset.Contains(action);
        }

        public IEnumerator<InputAction> GetEnumerator()
        {
            return asset.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public void Enable()
        {
            asset.Enable();
        }

        public void Disable()
        {
            asset.Disable();
        }

        // WaveController
        private readonly InputActionMap m_WaveController;
        private IWaveControllerActions m_WaveControllerActionsCallbackInterface;
        private readonly InputAction m_WaveController_BeatMultiplier;
        private readonly InputAction m_WaveController_BandSelector;
        private readonly InputAction m_WaveController_ShowHideUi;
        public struct WaveControllerActions
        {
            private @WaveInput m_Wrapper;
            public WaveControllerActions(@WaveInput wrapper) { m_Wrapper = wrapper; }
            public InputAction @BeatMultiplier => m_Wrapper.m_WaveController_BeatMultiplier;
            public InputAction @BandSelector => m_Wrapper.m_WaveController_BandSelector;
            public InputAction @ShowHideUi => m_Wrapper.m_WaveController_ShowHideUi;
            public InputActionMap Get() { return m_Wrapper.m_WaveController; }
            public void Enable() { Get().Enable(); }
            public void Disable() { Get().Disable(); }
            public bool enabled => Get().enabled;
            public static implicit operator InputActionMap(WaveControllerActions set) { return set.Get(); }
            public void SetCallbacks(IWaveControllerActions instance)
            {
                if (m_Wrapper.m_WaveControllerActionsCallbackInterface != null)
                {
                    @BeatMultiplier.started -= m_Wrapper.m_WaveControllerActionsCallbackInterface.OnBeatMultiplier;
                    @BeatMultiplier.performed -= m_Wrapper.m_WaveControllerActionsCallbackInterface.OnBeatMultiplier;
                    @BeatMultiplier.canceled -= m_Wrapper.m_WaveControllerActionsCallbackInterface.OnBeatMultiplier;
                    @BandSelector.started -= m_Wrapper.m_WaveControllerActionsCallbackInterface.OnBandSelector;
                    @BandSelector.performed -= m_Wrapper.m_WaveControllerActionsCallbackInterface.OnBandSelector;
                    @BandSelector.canceled -= m_Wrapper.m_WaveControllerActionsCallbackInterface.OnBandSelector;
                    @ShowHideUi.started -= m_Wrapper.m_WaveControllerActionsCallbackInterface.OnShowHideUi;
                    @ShowHideUi.performed -= m_Wrapper.m_WaveControllerActionsCallbackInterface.OnShowHideUi;
                    @ShowHideUi.canceled -= m_Wrapper.m_WaveControllerActionsCallbackInterface.OnShowHideUi;
                }
                m_Wrapper.m_WaveControllerActionsCallbackInterface = instance;
                if (instance != null)
                {
                    @BeatMultiplier.started += instance.OnBeatMultiplier;
                    @BeatMultiplier.performed += instance.OnBeatMultiplier;
                    @BeatMultiplier.canceled += instance.OnBeatMultiplier;
                    @BandSelector.started += instance.OnBandSelector;
                    @BandSelector.performed += instance.OnBandSelector;
                    @BandSelector.canceled += instance.OnBandSelector;
                    @ShowHideUi.started += instance.OnShowHideUi;
                    @ShowHideUi.performed += instance.OnShowHideUi;
                    @ShowHideUi.canceled += instance.OnShowHideUi;
                }
            }
        }
        public WaveControllerActions @WaveController => new WaveControllerActions(this);
        public interface IWaveControllerActions
        {
            void OnBeatMultiplier(InputAction.CallbackContext context);
            void OnBandSelector(InputAction.CallbackContext context);
            void OnShowHideUi(InputAction.CallbackContext context);
        }
    }
}
