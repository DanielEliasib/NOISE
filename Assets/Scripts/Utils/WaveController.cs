using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using AL.NOISE.InputSystem;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using System.Linq;
using TMPro;

public class WaveController : MonoBehaviour
{
    WaveInput _MainInput;
    [SerializeField] private LayerManager _LayerManager;
    [SerializeField] private RawImage _WaveTarget;
    [SerializeField] private ComputeShader _WaveRenderer;
    [SerializeField] private Canvas _UI;
    [SerializeField] private TextMeshProUGUI _TextHolder;

    private WaveDrawer _WaveDrawer;
    private bool _ActiveUI;
    private string _DummyString = "Wave[{0}] threshold: {1}";

    int _BeatIndex;

    private void Awake()
    {
        _MainInput = new WaveInput();
        _MainInput.Enable();
        _BeatIndex = 0;

        _WaveDrawer = new WaveDrawer(8 * 12, 8 * 6, _WaveRenderer, _WaveTarget);

        _ActiveUI = false;
        _UI.gameObject.SetActive(_ActiveUI);
    }

    // Start is called before the first frame update
    void Start()
    {
        _MainInput.WaveController.BeatMultiplier.performed += ScrollListener;
        _MainInput.WaveController.BandSelector.performed += ArrowListener;
        _MainInput.WaveController.ShowHideUi.performed += DebugUI;
        _MainInput.WaveController.ChangeColor.performed += ChangeColorListener;
    }

    // Update is called once per frame
    void Update()
    {
        if (_ActiveUI)
        {
            var _Spectrum = _LayerManager._Spectrum;
            var _LongProms = _LayerManager._LongProms;
            var _BeatMultipliers = _LayerManager._BeatMultipliers;

            if (_Spectrum != null)
                if (_Spectrum[_BeatIndex] != null)
                    _WaveDrawer.SetComputeData
                        (_Spectrum[_BeatIndex].ToArray(),
                        _LongProms[_BeatIndex],
                        Mathf.Min(_BeatMultipliers[_BeatIndex] * _LongProms[_BeatIndex], _LongProms[_BeatIndex] + 1));
            
            if(_BeatMultipliers != null)
            {
                _TextHolder.SetText(string.Format(_DummyString, _BeatIndex, _BeatMultipliers[_BeatIndex]));
            }
        }
        
    }

    private void OnDisable()
    {
        _MainInput.Disable();
    }

    void ScrollListener(InputAction.CallbackContext context)
    {
        var value = context.ReadValue<Vector2>();
        if(_LayerManager != null)
        {
            var bMult = _LayerManager._BeatMultipliers[_BeatIndex] + value.y * 0.025f;
            _LayerManager._BeatMultipliers[_BeatIndex] = Mathf.Clamp(bMult, 1.0f, 10.0f);
        }
        Debug.Log("Val: " + value);
    }

    void ArrowListener(InputAction.CallbackContext context)
    {
        var value = (int)context.ReadValue<float>();
        if (_LayerManager != null)
        {
            _BeatIndex += value;
            _BeatIndex = _BeatIndex % _LayerManager._BeatMultipliers.Length;
            _BeatIndex = _BeatIndex < 0 ? _LayerManager._BeatMultipliers.Length + _BeatIndex : _BeatIndex;
        }

        Debug.Log("Arrow: " + _BeatIndex);
    }

    void ChangeColorListener(InputAction.CallbackContext context)
    {
        _LayerManager.NextColorScheme();
    }

    void DebugUI(InputAction.CallbackContext context)
    {
        _ActiveUI = !_ActiveUI;
        _UI.gameObject.SetActive(_ActiveUI);
    }
}
