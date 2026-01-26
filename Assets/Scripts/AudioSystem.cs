using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using FMODUnity;
using System.Threading;
using Unity.VisualScripting;
using FMOD.Studio;

public class AudioSystem : MonoBehaviour
{
    // EMITTERS //
    [SerializeField] private StudioEventEmitter TavernMusic_2;

    public FMODUnity.StudioEventEmitter TavernMusic;
    public StudioEventEmitter TavernAmb;
    public StudioEventEmitter TavernFireplace;
    public StudioEventEmitter OutsideAmb;

    // EVENTS //
    [SerializeField] private EventReference doorsEvent_2;
    private EventInstance DoorsSound_2;

    FMOD.Studio.EventInstance DoorsSound;
    public EventReference doorsEvent;
    FMOD.Studio.EventInstance FootstepsSound;
    public EventReference footstepsEvent;
    FMOD.Studio.EventInstance JumpSound;
    public EventReference jumpEvent;
    FMOD.Studio.EventInstance LandSound;
    public EventReference landEvent;

    // --- SEKCJA ZAKLĘĆ ---
    public FMOD.Studio.EventInstance SpellSound; // Instancja ładowania (Charge)

    public EventReference spellEvent;       // 1. Ładowanie (Charge Loop)
    public EventReference spellLaunchEvent; // 2. Strzał (Launch One-Shot)

    // NOWE: Dźwięk anulowania/fizzle
    public EventReference spellCancelEvent; // 3. Anulowanie (Cancel One-Shot)

    public FMOD.Studio.EventInstance SpellImpact;
    public EventReference spellImpactEvent;


    // SNAPSHOTS //
    FMOD.Studio.EventInstance InsideRoom;
    public EventReference insideRoomSnap;
    public FMOD.Studio.EventInstance Outside;
    public EventReference outsideSnapshot;
    FMOD.Studio.EventInstance HealthSnapshot;
    public EventReference healthSnapshot;

    // VCA // 
    public FMOD.Studio.VCA GlobalVCA;
    public FMOD.Studio.VCA MusicVCA;
    public FMOD.Studio.VCA TavernVCA;
    public FMOD.Studio.VCA OutsideVCA;

    // STRING NAMES // 
    private string footsteps_surface;
    public string doorsName;
    private string open;
    private string close;
    private string door_1;
    private string door_2;
    private string door_3;

    // FLAGS // 
    private bool doorsOpened_1;
    private bool doorsOpened_2;
    private bool doorsOpened_3;
    public bool isGrounded = true;
    private bool isJumping = false;
    private bool outsideSnapActivated = false;
    public bool roomsAmbientActivated;
    public bool isMusicPlaying = true;
    public bool muteActive;
    public bool musicMuteActive;
    public bool tavernMuteActive;
    public bool outsideMuteActive;
    private bool healthSnapActive;
    private PLAYBACK_STATE spell_pb;

    public float distToGround;

    void Start()
    {
        // VCA SETUP //
        GlobalVCA = FMODUnity.RuntimeManager.GetVCA("vca:/Mute");
        MusicVCA = FMODUnity.RuntimeManager.GetVCA("vca:/Music");
        TavernVCA = FMODUnity.RuntimeManager.GetVCA("vca:/Tavern_amb");
        OutsideVCA = FMODUnity.RuntimeManager.GetVCA("vca:/Outside_amb");

        // START SETUP //
        doorsOpened_1 = true;
        doorsOpened_2 = true;
        doorsOpened_3 = true;
        muteActive = false;
        isGrounded = true;
        isJumping = false;
        outsideSnapActivated = false;
        healthSnapActive = false;
        footsteps_surface = "Footsteps_surface";
        open = "Open";
        close = "Close";
        door_1 = "Tavern_door_room";
        door_2 = "Tavern_door_room (1)";
        door_3 = "Tavern_door_room (2)";

        distToGround = GetComponent<Collider>().bounds.extents.y;

        if (TavernFireplace == null)
            Debug.LogError("NULL");
    }

    // ... (Funkcje IsGrounded, DoorsManager, Footsteps, Jump, Land - BEZ ZMIAN) ...

    public bool IsGrounded() { return Physics.Raycast(transform.position, Vector3.down, distToGround + 0.5f); }
    public float DecibelToLinear(float dB) { float linear = Mathf.Pow(10.0f, dB / 20f); return linear; }
    public void RoomsAmbientON() { roomsAmbientActivated = true; }
    public void RoomsAmbientOFF() { roomsAmbientActivated = false; }
    public void FireplaceOFF() { TavernFireplace.SetParameter("Fire", 0); }
    public void FireplaceON() { TavernFireplace.SetParameter("Fire", 1); }

    void DoorsManager(ref FMOD.Studio.EventInstance doorSoundInstance, int doorsNumber, string doorState)
    {
        doorSoundInstance = FMODUnity.RuntimeManager.CreateInstance(doorsEvent);
        doorSoundInstance.setParameterByNameWithLabel("Doors", doorState);
        doorSoundInstance.set3DAttributes(FMODUnity.RuntimeUtils.To3DAttributes(gameObject.transform));
        doorSoundInstance.start();
        doorSoundInstance.release();

        if (doorsNumber == 1) doorsOpened_1 = !doorsOpened_1;
        else if (doorsNumber == 2) doorsOpened_2 = !doorsOpened_2;
        else if (doorsNumber == 3) doorsOpened_3 = !doorsOpened_3;
    }
    public void PlayDoorSound()
    {
        if (doorsName == door_1) { if (doorsOpened_1) DoorsManager(ref DoorsSound, 1, close); else DoorsManager(ref DoorsSound, 1, open); }
        else if (doorsName == door_2) { if (doorsOpened_2) DoorsManager(ref DoorsSound, 2, close); else DoorsManager(ref DoorsSound, 2, open); }
        else if (doorsName == door_3) { if (doorsOpened_3) DoorsManager(ref DoorsSound, 3, close); else DoorsManager(ref DoorsSound, 3, open); }
    }

    public void PlayFootsteps()
    {
        RaycastHit hit;
        if (Physics.Raycast(transform.position, Vector3.down, out hit, distToGround + 0.5f))
        {
            string surfaceType = "Stone";
            switch (hit.collider.tag)
            {
                case "Wood": surfaceType = "Wood"; break;
                case "Stone": case "Outside": case "Inside_stone": surfaceType = "Stone"; break;
                case "ground": surfaceType = "Ground"; break;
            }
            FootstepsSound = FMODUnity.RuntimeManager.CreateInstance(footstepsEvent);
            FootstepsSound.set3DAttributes(FMODUnity.RuntimeUtils.To3DAttributes(gameObject.transform));
            FootstepsSound.setParameterByNameWithLabel(footsteps_surface, surfaceType);
            FootstepsSound.start();
            FootstepsSound.release();
        }
    }

    public void PlayJump()
    {
        if (IsGrounded())
        {
            JumpSound = FMODUnity.RuntimeManager.CreateInstance(jumpEvent);
            if (IsGrounded())
            {
                RaycastHit hit;
                if (Physics.Raycast(transform.position, Vector3.down, out hit, distToGround + 0.5f))
                {
                    string surface = hit.collider.tag switch { "Stone" => "Stone", "Wood" => "Wood", "Inside_stone" => "Stone", "ground" => "Ground", "Bed" => "Bed", _ => "Stone" };
                    JumpSound.setParameterByNameWithLabel(footsteps_surface, surface);
                    JumpSound.start();
                }
            }
            JumpSound.release();
            isGrounded = false;
            isJumping = true;
        }
    }

    public void PlayLanding()
    {
        if (IsGrounded() && !isGrounded)
        {
            if (isJumping)
            {
                LandSound = FMODUnity.RuntimeManager.CreateInstance(landEvent);
                RaycastHit hit;
                if (Physics.Raycast(transform.position, Vector3.down, out hit, distToGround + 0.5f))
                {
                    string surface = hit.collider.tag switch { "Stone" => "Stone", "Wood" => "Wood", "Inside_stone" => "Stone", "ground" => "Ground", "Bed" => "Bed", _ => "Stone" };
                    LandSound.setParameterByNameWithLabel(footsteps_surface, surface);
                    LandSound.start();
                }
                LandSound.release();
                isGrounded = true;
                isJumping = false;
            }
        }
    }

    // --- METODY ZAKLĘĆ ---

    public void SpellCast()
    {
        // START ŁADOWANIA (Charge Loop)
        if (!string.IsNullOrEmpty(spellEvent.Path))
        {
            SpellSound = RuntimeManager.CreateInstance(spellEvent);
            SpellSound.set3DAttributes(RuntimeUtils.To3DAttributes(gameObject));
            SpellSound.start();
        }
    }

    public void SpellRelease()
    {
        // 1. ZATRZYMAJ ŁADOWANIE (z FadeOutem zdefiniowanym w AHDSR)
        if (SpellSound.isValid())
        {
            SpellSound.stop(FMOD.Studio.STOP_MODE.ALLOWFADEOUT);
            SpellSound.release();
        }

        // 2. ODTWÓRZ STRZAŁ (Launch One-Shot)
        if (!string.IsNullOrEmpty(spellLaunchEvent.Path))
        {
            RuntimeManager.PlayOneShotAttached(spellLaunchEvent, gameObject);
        }
    }

    public void SpellCancel()
    {
        // 1. ZATRZYMAJ ŁADOWANIE (z FadeOutem)
        if (SpellSound.isValid())
        {
            SpellSound.stop(FMOD.Studio.STOP_MODE.ALLOWFADEOUT);
            SpellSound.release();
        }

        // 2. ODTWÓRZ DŹWIĘK ANULOWANIA (Cancel One-Shot) - NOWOŚĆ
        if (!string.IsNullOrEmpty(spellCancelEvent.Path))
        {
            // Odtwarzamy dźwięk zrezygnowania (fizzle) w miejscu gracza
            RuntimeManager.PlayOneShotAttached(spellCancelEvent, gameObject);
        }
    }

    public void SpellImpactSound(Vector3 position)
    {
        RuntimeManager.PlayOneShot(spellImpactEvent, position);
    }

    // ... (RESZTA KODU: Snapshots, VCA - BEZ ZMIAN) ...

    public void OutsideSnap()
    {
        RaycastHit hit;
        if (Physics.Raycast(transform.position, Vector3.down, out hit, distToGround + 0.5f))
        {
            if (hit.collider.CompareTag("Outside") && outsideSnapActivated == false)
            {
                Outside = FMODUnity.RuntimeManager.CreateInstance(outsideSnapshot);
                Outside.start();
                outsideSnapActivated = !outsideSnapActivated;
            }
            else if (hit.collider.CompareTag("Inside_stone") && outsideSnapActivated == true)
            {
                Outside.stop(FMOD.Studio.STOP_MODE.ALLOWFADEOUT);
                Outside.release();
                outsideSnapActivated = !outsideSnapActivated;
            }
        }
    }

    private void RoomsSnapInstanceStart() { InsideRoom.start(); InsideRoom.release(); }
    private void RoomsSnapInstanceStop() { InsideRoom.stop(FMOD.Studio.STOP_MODE.ALLOWFADEOUT); InsideRoom.release(); }

    public void RoomsSnap()
    {
        if (!InsideRoom.isValid()) InsideRoom = FMODUnity.RuntimeManager.CreateInstance(insideRoomSnap);
        if (roomsAmbientActivated == true && doorsName == door_1 && doorsOpened_1 == false) RoomsSnapInstanceStart();
        else if (roomsAmbientActivated == true && doorsName == door_2 && doorsOpened_2 == false) RoomsSnapInstanceStart();
        else if (roomsAmbientActivated == true && doorsName == door_3 && doorsOpened_3 == false) RoomsSnapInstanceStart();
        else RoomsSnapInstanceStop();
    }

    public void HealthSnap()
    {
        if (!healthSnapActive)
        {
            HealthSnapshot = FMODUnity.RuntimeManager.CreateInstance(healthSnapshot);
            HealthSnapshot.start();
            healthSnapActive = !healthSnapActive;
        }
        else if (healthSnapActive)
        {
            HealthSnapshot.stop(FMOD.Studio.STOP_MODE.ALLOWFADEOUT);
            HealthSnapshot.release();
            healthSnapActive = !healthSnapActive;
        }
    }

    public void ToggleMute(KeyCode key, ref bool muteActive, FMOD.Studio.VCA vca)
    {
        if (Input.GetKeyDown(key))
        {
            float volume = muteActive ? 0 : -100;
            vca.setVolume(DecibelToLinear(volume));
            muteActive = !muteActive;
        }
    }
}