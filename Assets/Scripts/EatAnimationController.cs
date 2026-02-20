// The EatAnimationController manages the eating behavior of a character. It moves the character to the food, scales and positions the food object, and plays an eating animation. After eating, it resets the food and updates the character's state. It also interacts with UI buttons and external systems for state updates.

using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TapHouse.Logging;

public class EatAnimationController : MonoBehaviour
{
    [SerializeField] private DogController characterController; // Manages dog animations and behaviors
    [SerializeField] private FirebaseManager _firebaseManager; // Updates pet state in Firebase
    [SerializeField] private HungerManager _hungryManager; // Manages hunger state
    [SerializeField] private TurnAndMoveHandler _turnAndMoveHandler; // Handles dog movement
    [SerializeField] private GameObject foodObject; // The food object used during the eating animation
    [SerializeField] private Transform foodContentTransform; // The food content inside the bowl (Food_1)
    [SerializeField] private float foodDepletionDepth = 0.05f; // How far the food sinks when eaten
    [SerializeField] private float maxEatDuration = 20f; // Maximum duration for eating
    [SerializeField] private float minEatDuration = 10f; // Minimum duration for eating
    [SerializeField] private MainUIButtons _mainUiButtons;
    [SerializeField] private DogStateController _dogStateController;

    private Vector3 originalFoodPosition; // Original position of the food object
    private Vector3 originalFoodScale; // Original scale of the food object
    private Vector3 originalFoodContentPosition; // Original position of the food content
    private void Awake()
    {
        if (foodObject == null)
        {
            GameLogger.LogError(LogCategory.Dog,"Food object is not assigned in the Inspector");
            enabled = false;
            return;
        }

        originalFoodPosition = foodObject.transform.position;
        originalFoodScale = foodObject.transform.localScale;

        if (foodContentTransform != null)
        {
            originalFoodContentPosition = foodContentTransform.localPosition;
        }
    }


    private void Start()
    {
        foodObject.SetActive(false);
    }
    public void StartEatingAnimation(float feedScale = 1f)
    {
        StartCoroutine(AnimeEating(feedScale));
    }

    public IEnumerator AnimeEating(float feedScale)
    {
        if (foodObject == null) yield break;

        // Update pet state to feeding
        GlobalVariables.CurrentState = PetState.feeding;
        GlobalVariables.AttentionCount = 10;
        characterController.ActionBool(false);

        // Calculate the scale and duration of the eating animation
        int randomValue = UnityEngine.Random.Range(1, 11);
        float eatDuration = Mathf.Lerp(minEatDuration, maxEatDuration, (randomValue - 1) / 9f);

        // Activate and adjust the food object
        foodObject.SetActive(true);
        _mainUiButtons.UpdateButtonVisibility(false);

        // Move the dog to the food position
        _turnAndMoveHandler.StartTurnAndMove(Vector3.zero, 1f, PetState.feeding);
        // Start eating animation
        characterController.isEating(true);

        // Gradually lower the food content to simulate eating
        float elapsed = 0f;
        while (elapsed < eatDuration)
        {
            elapsed += Time.deltaTime;
            float progress = Mathf.Clamp01(elapsed / eatDuration);

            if (foodContentTransform != null)
            {
                foodContentTransform.localPosition = originalFoodContentPosition -
                    new Vector3(0, foodDepletionDepth * progress, 0);
            }

            yield return null;
        }

        foodObject.SetActive(false);
        foodObject.transform.position = originalFoodPosition;
        foodObject.transform.localScale = originalFoodScale;

        // Reset food content position
        if (foodContentTransform != null)
        {
            foodContentTransform.localPosition = originalFoodContentPosition;
        }


        characterController.isEating(false);
        long unixTimeNow = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        _hungryManager?.UpdateLastEatTime(unixTimeNow);

        UpdateFirebaseState();

        _mainUiButtons.UpdateButtonVisibility(true);
        GlobalVariables.AttentionCount = 0;
        GlobalVariables.CurrentState = PetState.idle;
    }
    private void UpdateFirebaseState()
    {
        _firebaseManager?.UpdatePetState("idle");
        _firebaseManager?.UpdateLog("feed");
        _dogStateController.OnFeed();
    }

}
