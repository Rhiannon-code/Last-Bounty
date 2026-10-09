using FPSParkour.Bounty;
using UnityEngine;
using UnityEngine.UI;

namespace FPSParkour.UI
{
    [DisallowMultipleComponent]
    public class ContractResolutionView : MonoBehaviour
    {
        [SerializeField] BountyRegistry registry;
        [SerializeField] GameObject root;
        [SerializeField] Text targetLabel;
        [SerializeField] Text chargesLabel;
        [SerializeField] Button captureButton;
        [SerializeField] Button killButton;
        [SerializeField] Button releaseButton;

        BountyTarget current;

        void Awake()
        {
            captureButton?.onClick.AddListener(Capture);
            killButton?.onClick.AddListener(Kill);
            releaseButton?.onClick.AddListener(Release);
            Close();
        }

        void OnEnable() => BountyTarget.AnyResolutionRequested += Open;

        void OnDisable() => BountyTarget.AnyResolutionRequested -= Open;

        public void Open(BountyTarget target)
        {
            if (target == null || target.Contract == null)
                return;

            current = target;

            if (targetLabel != null) targetLabel.text = target.Contract.TargetName;
            if (chargesLabel != null) chargesLabel.text = target.Contract.Charges;

            if (releaseButton != null)
                releaseButton.gameObject.SetActive(registry != null && registry.ReleaseUnlocked);

            if (root != null)
                root.SetActive(true);
        }

        public void Close()
        {
            current = null;

            if (root != null)
                root.SetActive(false);
        }

        void Capture()
        {
            current?.Capture();
            Close();
        }

        void Kill()
        {
            current?.Execute();
            Close();
        }

        void Release()
        {
            current?.Release();
            Close();
        }
    }
}
