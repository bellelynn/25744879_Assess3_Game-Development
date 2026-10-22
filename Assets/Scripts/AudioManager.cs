using UnityEngine;

public class AudioManager : MonoBehaviour
{
    [Header("Music")]
    [SerializeField] private AudioClip intro;
    [SerializeField] private AudioClip startScene;
    [SerializeField] private AudioClip ghostNormal;
    [SerializeField] private AudioClip ghostScared;
    [SerializeField] private AudioClip ghostDead;

    [Header("SFX")]
    [SerializeField] private AudioClip pacStudentMove;
    [SerializeField] private AudioClip pelletEat;
    [SerializeField] private AudioClip ghostEat;
    [SerializeField] private AudioClip bonusEat;
    [SerializeField] private AudioClip wallHit;
    [SerializeField] private AudioClip pacStudentDeath;

    [Header("Volume")]
    [Range(0f, 1f)]
    [SerializeField] private float introVolume = 0.4f;

    [Range(0f, 1f)]
    [SerializeField] private float normalVolume = 0.2f;

    [Range(0f, 1f)]
    [SerializeField] private float scaredVolume = 0.3f;

    [Range(0f, 1f)]
    [SerializeField] private float deadVolume = 0.3f;

    [Range(0f, 1f)]
    [SerializeField] private float movementVolume = 0.5f;

    [Range(0f, 1f)]
    [SerializeField] private float sfxVolume = 0.5f;

    [Header("SFX Lengths (seconds)")]
    [SerializeField] private float wallHitLength = 0.5f;
    [SerializeField] private float shortSfxLength = 1f;
    [SerializeField] private float deathSfxLength = 2f;

    [Header("Scene")]
    [SerializeField] private bool isStartScene = false;

    private AudioSource musicSource;
    private AudioSource movementSource;
    private AudioSource sfxSource;


    void Awake()
    {
        //Create the AudioSources
        musicSource = gameObject.AddComponent<AudioSource>();
        movementSource = gameObject.AddComponent<AudioSource>();
        sfxSource = gameObject.AddComponent<AudioSource>();

        musicSource.spatialBlend = 0f;
        movementSource.spatialBlend = 0f;
        sfxSource.spatialBlend = 0f;
    }

    void Start()
    {
        if (isStartScene)
        {
            PlayStartScene();
        }
    }

    public void PlayIntro()
    {
        if (intro == null)
            return;

        musicSource.clip = intro;
        musicSource.volume = introVolume;
        musicSource.loop = false;
        musicSource.Play();
    }

    public void PlayStartScene()
    {
        PlayMusic(startScene, introVolume);
    }

    public void PlayGhostNormal()
    {
        PlayMusic(ghostNormal, normalVolume);
    }

    public void PlayGhostScared()
    {
        PlayMusic(ghostScared, scaredVolume);
    }

    public void PlayGhostDead()
    {
        PlayMusic(ghostDead, deadVolume);
    }

    private void PlayMusic(AudioClip clip, float volume)
    {
        if (clip == null)
            return;

        musicSource.Stop();
        musicSource.clip = clip;
        musicSource.volume = volume;
        musicSource.loop = true;
        musicSource.Play();
    }

    public void PlayMovement()
    {
        if (pacStudentMove == null)
            return;

        movementSource.clip = pacStudentMove;
        movementSource.volume = movementVolume;
        movementSource.loop = true;
        movementSource.Play();
    }

    public void PlayMoveLoop(bool eating)
    {
        AudioClip clip = eating ? pelletEat : pacStudentMove;

        if (clip == null)
            return;

        if (movementSource.isPlaying && movementSource.clip == clip)
            return;

        movementSource.clip = clip;
        movementSource.volume = movementVolume;
        movementSource.loop = true;
        movementSource.Play();
    }

    public void StopMovement()
    {
        movementSource.Stop();
    }

    public void PlayPelletEat()
    {
        PlaySFX(pelletEat, shortSfxLength);
    }

    public void PlayGhostEat()
    {
        PlaySFX(ghostEat, shortSfxLength);
    }

    public void PlayBonusEat()
    {
        PlaySFX(bonusEat, shortSfxLength);
    }

    public void PlayWallHit()
    {
        PlaySFX(wallHit, wallHitLength);
    }

    public void PlayPacStudentDeath()
    {
        PlaySFX(pacStudentDeath, deathSfxLength);
    }

    // The source clips are long, so each effect plays on its own
    // temporary AudioSource that is removed after maxLength seconds.
    private void PlaySFX(AudioClip clip, float maxLength)
    {
        if (clip == null)
            return;

        GameObject sfxObject = new GameObject("SFX_" + clip.name);
        sfxObject.transform.SetParent(transform);

        AudioSource source = sfxObject.AddComponent<AudioSource>();
        source.spatialBlend = 0f;
        source.volume = sfxVolume;
        source.clip = clip;
        source.Play();

        Destroy(sfxObject, Mathf.Min(maxLength, clip.length));
    }
}