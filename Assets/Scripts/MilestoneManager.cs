using System.Collections.Generic;
using UnityEngine;

public class MilestoneManager : MonoBehaviour
{
    public static MilestoneManager Instance { get; private set; }

    [Header("Milestone Settings")]
    public List<int> milestoneTiers = new List<int> { 100, 300, 600, 1000, 1500, 2100 };
    public int baseInfiniteStep = 1000; 
    public float infiniteStepMultiplier = 1.25f; 

    public int CurrentMilestoneIndex { get; private set; } = 0;
    public int CurrentTargetMilestone { get; private set; }
    
    private int currentInfiniteStep;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    private void Start()
    {
        currentInfiniteStep = baseInfiniteStep;
        SetInitialTarget();
    }

    private void SetInitialTarget()
    {
        if (milestoneTiers.Count > 0)
            CurrentTargetMilestone = milestoneTiers[0];
        else
            CurrentTargetMilestone = currentInfiniteStep;
    }

    public void CheckMilestone(int totalScore)
    {
        while (totalScore >= CurrentTargetMilestone)
        {
            CurrentMilestoneIndex++;
            
            if (CurrentMilestoneIndex < milestoneTiers.Count)
            {
                CurrentTargetMilestone = milestoneTiers[CurrentMilestoneIndex];
            }
            else
            {
                CurrentTargetMilestone += currentInfiniteStep;
                currentInfiniteStep = Mathf.RoundToInt(currentInfiniteStep * infiniteStepMultiplier); 
            }

            Debug.Log($"[LEVEL UP] Milestone ke-{CurrentMilestoneIndex} Tercapai! Target baru: {CurrentTargetMilestone} (Step selanjutnya butuh: {currentInfiniteStep})");
            TriggerMilestoneRewards();
        }
    }

    private void TriggerMilestoneRewards()
    {
        if (GridManager.Instance != null)
        {
            GridManager.Instance.currentMilestone++;
            GridManager.Instance.ExpandGridBasedOnProgression();
        }

        if (BlockQueueManager.Instance != null)
        {
            BlockQueueManager.Instance.OnMilestoneReached(CurrentMilestoneIndex);
        }
    }

    public void ResetMilestones()
    {
        CurrentMilestoneIndex = 0;
        currentInfiniteStep = baseInfiniteStep;
        SetInitialTarget();
    }
}