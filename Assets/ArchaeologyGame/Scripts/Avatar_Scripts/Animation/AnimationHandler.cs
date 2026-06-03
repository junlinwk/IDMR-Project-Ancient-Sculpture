using UnityEngine;

public class AnimationHandler : MonoBehaviour
{
    private AudioPlayer audioPlayer;
    private Animator animator;
    private bool lastIsTalking;
    private Coroutine typingRoutine;
    
    void Start()
    {
        animator = GetComponent<Animator>();
        if (animator == null)
        {
            animator = GetComponentInChildren<Animator>();
        }
        audioPlayer = FindObjectOfType<AudioPlayer>();
        lastIsTalking = false;
    }

    void Update()
    {
        // TODO: Handle the animation transitions based on your logics
        
        bool isTalking = audioPlayer != null && audioPlayer.IsAudioPlaying();

        if (isTalking && !lastIsTalking)
        {
            int choice = Random.Range(0, 3);

            animator.SetBool("isAngry", false);
            animator.SetBool("isThoughtful", false);
            animator.SetBool("isSurprised", false);

            if (choice == 0)
            {
                animator.SetBool("isAngry", true);
            }
            else if (choice == 1)
            {
                animator.SetBool("isThoughtful", true);
            }
            else
            {
                animator.SetBool("isSurprised", true);
            }
        }

        if (!isTalking && lastIsTalking)
        {
            animator.SetBool("isAngry", false);
            animator.SetBool("isThoughtful", false);
            animator.SetBool("isSurprised", false);
        }

        lastIsTalking = isTalking;
    }

    public void SetSurprised(bool isSurprised)
    {
        if (animator != null)
        {
            animator.SetBool("isSurprised", isSurprised);
        }
    }

    public void PlayTypingAnimation()
    {
        if (animator == null)
        {
            return;
        }

        animator.SetBool("isTalking", true);

        if (typingRoutine != null)
        {
            StopCoroutine(typingRoutine);
        }

        typingRoutine = StartCoroutine(ClearTypingAfterDelay());
    }

    private System.Collections.IEnumerator ClearTypingAfterDelay()
    {
        yield return new WaitForSeconds(1.5f);

        if (animator != null)
        {
            animator.SetBool("isTalking", false);
        }

        typingRoutine = null;
    }
}
