using System.Collections;
using UnityEngine;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;

namespace IndoorNav.Navigation
{
    /// <summary>
    /// Configures AR Foundation's environment-depth occlusion at runtime, with
    /// a graceful fallback when the device doesn't support depth. After
    /// <see cref="Start"/> runs and the negotiation delay elapses,
    /// <see cref="IsDepthAvailable"/> reflects whether the device actually
    /// delivered a non-Disabled depth mode.
    ///
    /// The companion shader (AROccludedArrow) samples the environment depth
    /// texture AR Foundation auto-binds when an <see cref="AROcclusionManager"/>
    /// is active — so this script's only job is to flip the right switches on
    /// the manager.
    /// </summary>
    public class AROcclusionSetup : MonoBehaviour
    {
        [Tooltip("Reference to the AROcclusionManager on the AR Camera. Falls back to FindAnyObjectByType when unassigned.")]
        [SerializeField] private AROcclusionManager occlusionManager;

        [Tooltip("Environment depth mode. 'Best' enables full depth-based occlusion. 'Disabled' turns occlusion off (fallback for unsupported devices).")]
        [SerializeField] private EnvironmentDepthMode depthMode = EnvironmentDepthMode.Best;

        [Tooltip("Enable temporal smoothing on the depth image. Smoother but slightly more lag.")]
        [SerializeField] private bool useTemporalSmoothing = true;

        [Tooltip("Log a clear message at startup indicating whether depth occlusion is active.")]
        [SerializeField] private bool logStatusOnStart = true;

        [Tooltip("Seconds to wait after applying settings before sampling the actual mode. ARCore needs a moment to negotiate the depth subsystem.")]
        [SerializeField, Min(0.1f)] private float statusCheckDelay = 1f;

        /// <summary>True after Start has confirmed the device delivered a non-Disabled depth mode.</summary>
        public bool IsDepthAvailable { get; private set; }

        /// <summary>The most recent status string set by this component. Useful for surfacing in UI.</summary>
        public string StatusMessage { get; private set; } = string.Empty;

        private void Start()
        {
            if (occlusionManager == null)
                occlusionManager = FindAnyObjectByType<AROcclusionManager>();

            if (occlusionManager == null)
            {
                StatusMessage =
                    "[AROcclusionSetup] No AROcclusionManager found on the camera. " +
                    "Add one to the AR Camera GameObject under XR Origin → Camera Offset.";
                Debug.LogWarning(StatusMessage);
                return;
            }

            occlusionManager.requestedEnvironmentDepthMode = depthMode;
            occlusionManager.environmentDepthTemporalSmoothingRequested = useTemporalSmoothing;

            StartCoroutine(CheckActiveModeAfterDelay());
        }

        private IEnumerator CheckActiveModeAfterDelay()
        {
            yield return new WaitForSeconds(statusCheckDelay);

            EnvironmentDepthMode current = occlusionManager.currentEnvironmentDepthMode;
            IsDepthAvailable = current != EnvironmentDepthMode.Disabled;

            if (IsDepthAvailable)
            {
                StatusMessage = $"[AROcclusionSetup] Depth occlusion: ACTIVE (mode={current})";
            }
            else
            {
                StatusMessage =
                    "[AROcclusionSetup] Depth occlusion: NOT AVAILABLE on this device. " +
                    $"Arrows will render without occlusion. (requested={depthMode}, current={current})";
            }

            if (logStatusOnStart)
                Debug.Log(StatusMessage);
        }

        /// <summary>
        /// Logs the supported environment-depth capabilities reported by the
        /// occlusion subsystem descriptor. Right-click the component header in
        /// Play mode to invoke. Call after the AR session has started — the
        /// descriptor is null until then.
        /// </summary>
        [ContextMenu("Check Depth Support")]
        public void CheckDepthSupport()
        {
            if (occlusionManager == null)
            {
                Debug.LogWarning("[AROcclusionSetup] AROcclusionManager not assigned.");
                return;
            }

            var descriptor = occlusionManager.descriptor;
            if (descriptor == null)
            {
                Debug.LogWarning(
                    "[AROcclusionSetup] No subsystem descriptor available — likely the AR session " +
                    "hasn't started yet, or the device doesn't have an occlusion subsystem.");
                return;
            }

            Debug.Log(
                "[AROcclusionSetup] Subsystem capabilities:\n" +
                $"  environmentDepthImageSupported              = {descriptor.environmentDepthImageSupported}\n" +
                $"  environmentDepthConfidenceImageSupported    = {descriptor.environmentDepthConfidenceImageSupported}\n" +
                $"  environmentDepthTemporalSmoothingSupported  = {descriptor.environmentDepthTemporalSmoothingSupported}\n" +
                $"  humanSegmentationStencilImageSupported      = {descriptor.humanSegmentationStencilImageSupported}\n" +
                $"  humanSegmentationDepthImageSupported        = {descriptor.humanSegmentationDepthImageSupported}\n" +
                $"  Currently requested: {occlusionManager.requestedEnvironmentDepthMode}\n" +
                $"  Currently delivered: {occlusionManager.currentEnvironmentDepthMode}");
        }
    }
}
