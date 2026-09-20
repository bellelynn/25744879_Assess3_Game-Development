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

    [Header("Scene")]
    [SerializeField] private bool isStartScene = false;

    private AudioSource musicSource;
    private AudioSource movementSource;
    private AudioSource sfxSource;

    private float introTimer;
    private bool introFinished;

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
        else
        {
            PlayIntro();
            PlayMovement();
        }
    }

    void Update()
    {
        //Intro music plays for 3 seconds
        if (!isStartScene && !introFinished)
        {
            introTimer += Time.deltaTime;

            if (!musicSource.isPlaying || introTimer >= 3f)
            {
                introFinished = true;
                PlayGhostNormal();
            }
        }
    }

    private void PlayIntro()
    {
        if (intro == null)
        {
            introFinished = true;
            PlayGhostNormal();
            return;
        }

        musicSource.clip = intro;
        musicSource.volume = introVolume;
        musicSource.loop = false;
        musicSource.Play();

        introTimer = 0f;
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

    public void StopMovement()
    {
        movementSource.Stop();
    }

    public void PlayPelletEat()
    {
        PlaySFX(pelletEat);
    }

    public void PlayGhostEat()
    {
        PlaySFX(ghostEat);
    }

    public void PlayBonusEat()
    {
        PlaySFX(bonusEat);
    }

    public void PlayWallHit()
    {
        PlaySFX(wallHit);
    }

    public void PlayPacStudentDeath()
    {
        PlaySFX(pacStudentDeath);
    }

    private void PlaySFX(AudioClip clip)
    {
        if (clip == null)
            return;

        sfxSource.PlayOneShot(clip, sfxVolume);
    }
}