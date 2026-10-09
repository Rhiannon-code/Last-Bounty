using System.Collections.Generic;
using FPSParkour.Bounty;
using UnityEngine;
using UnityEngine.UI;

namespace FPSParkour.UI
{
    [DisallowMultipleComponent]
    public class BountyBoardView : MonoBehaviour
    {
        [SerializeField] BountyBoard board;
        [SerializeField] GameObject root;
        [SerializeField] RectTransform content;
        [SerializeField] Button rowPrefab;
        [SerializeField] Text dossierLabel;

        readonly List<Button> rows = new List<Button>();

        void OnEnable()
        {
            if (board != null)
                board.BoardOpened += OnBoardOpened;

            SetVisible(false);
        }

        void OnDisable()
        {
            if (board != null)
                board.BoardOpened -= OnBoardOpened;
        }

        void OnBoardOpened(IReadOnlyList<BountyContract> available)
        {
            SetVisible(true);
            Clear();

            if (content == null || rowPrefab == null)
                return;

            foreach (BountyContract contract in available)
            {
                Button row = Instantiate(rowPrefab, content);

                Text label = row.GetComponentInChildren<Text>();
                if (label != null)
                    label.text = $"{contract.TargetName}  :  {contract.PayoutAlive} cr";

                BountyContract captured = contract;
                row.onClick.AddListener(() => Select(captured));
                rows.Add(row);
            }
        }

        void Select(BountyContract contract)
        {
            if (dossierLabel != null)
            {
                dossierLabel.text = $"{contract.TargetName}\n\n{contract.Charges}\n\n" +
                                    $"Alive: {contract.PayoutAlive} cr\nDead: {contract.PayoutDead} cr";
            }

            board?.Accept(contract);
        }

        void Clear()
        {
            foreach (Button row in rows)
            {
                if (row != null)
                    Destroy(row.gameObject);
            }

            rows.Clear();
        }

        void SetVisible(bool visible)
        {
            if (root != null)
                root.SetActive(visible);
        }
    }
}
