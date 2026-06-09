using System;
using vcv_etagere_remaster.Core.Interface;

namespace vcv_etagere_remaster.Core.Modules
{
    public enum AdsrState
    {
        Idle,
        Attack,
        Decay,
        Sustain,
        Release
    }

    /// <summary>
    /// Envelope generator (ADSR).
    /// Reads a gate signal on GateInput (> 0.5V = gate on),
    /// shapes the AudioInput signal and outputs it on AudioOutput.
    /// A raw 0–1 envelope CV is also available on EnvOutput.
    /// </summary>
    public class AdsrModule : IModule
    {
        public string Id   { get; } = Guid.NewGuid().ToString();
        public string Name => "ADSR";

        // --- Ports ---
        public IPort AudioInput  { get; }  // Signal à envelopper
        public IPort GateInput   { get; }  // Gate CV (> 0.5 = ON)
        public IPort AudioOutput { get; }  // Signal enveloppé
        public IPort EnvOutput   { get; }  // CV brut de l'enveloppe 0-1

        // --- Paramètres ADSR (en secondes / niveau) ---
        public float Attack  { get; set; } = 0.01f;
        public float Decay   { get; set; } = 0.2f;
        public float Sustain { get; set; } = 0.7f;
        public float Release { get; set; } = 0.3f;

        // --- État interne ---
        public AdsrState State        { get; private set; } = AdsrState.Idle;
        public float     CurrentLevel { get; private set; }

        private float _phaseSampleIndex;
        private float _releaseStartLevel;
        private bool  _wasGated;
        private bool  _manualGate;

        public AdsrModule()
        {
            AudioInput  = new SimplePort(Guid.NewGuid().ToString(), "In",   PortType.Input);
            GateInput   = new SimplePort(Guid.NewGuid().ToString(), "Gate", PortType.Input);
            AudioOutput = new SimplePort(Guid.NewGuid().ToString(), "Out",  PortType.Output);
            EnvOutput   = new SimplePort(Guid.NewGuid().ToString(), "Env",  PortType.Output);
        }

        // Déclenché manuellement depuis l'UI (bouton GATE)
        public void ManualGateOn()
        {
            _manualGate = true;
            TriggerGateOn();
        }

        public void ManualGateOff()
        {
            _manualGate = false;
            TriggerGateOff();
        }

        public void Process(float sampleRate)
        {
            // Détection montante/descendante de la gate via port CV
            bool gateHigh = GateInput.Value > 0.5f || _manualGate;
            if (gateHigh && !_wasGated)  TriggerGateOn();
            if (!gateHigh && _wasGated)  TriggerGateOff();
            _wasGated = gateHigh;

            float envelope = NextEnvelopeSample(sampleRate);

            AudioOutput.Value = AudioInput.Value * envelope;
            EnvOutput.Value   = envelope;
        }

        // ─────────────────────────────────────────────
        //  Gate logic
        // ─────────────────────────────────────────────

        private void TriggerGateOn()
        {
            State               = AdsrState.Attack;
            _phaseSampleIndex   = 0f;
            CurrentLevel        = 0f;
        }

        private void TriggerGateOff()
        {
            if (State != AdsrState.Idle)
            {
                State               = AdsrState.Release;
                _phaseSampleIndex   = 0f;
                _releaseStartLevel  = CurrentLevel;
            }
        }

        // ─────────────────────────────────────────────
        //  Envelope sample computation (même logique que le proto)
        // ─────────────────────────────────────────────

        private float NextEnvelopeSample(float sampleRate)
        {
            float level          = CurrentLevel;
            float attackSamples  = Math.Max(1f, Attack  * sampleRate);
            float decaySamples   = Math.Max(1f, Decay   * sampleRate);
            float releaseSamples = Math.Max(1f, Release * sampleRate);

            switch (State)
            {
                case AdsrState.Idle:
                    level = 0f;
                    break;

                case AdsrState.Attack:
                    _phaseSampleIndex += 1f;
                    level = _phaseSampleIndex / attackSamples;
                    if (level >= 1f)
                    {
                        level = 1f;
                        State = AdsrState.Decay;
                        _phaseSampleIndex = 0f;
                    }
                    break;

                case AdsrState.Decay:
                    _phaseSampleIndex += 1f;
                    float decayProgress = _phaseSampleIndex / decaySamples;
                    level = 1f + (Sustain - 1f) * decayProgress;
                    if (decayProgress >= 1f)
                    {
                        level = Sustain;
                        State = AdsrState.Sustain;
                        _phaseSampleIndex = 0f;
                    }
                    break;

                case AdsrState.Sustain:
                    level = Sustain;
                    break;

                case AdsrState.Release:
                    _phaseSampleIndex += 1f;
                    float releaseProgress = _phaseSampleIndex / releaseSamples;
                    level = _releaseStartLevel * (1f - releaseProgress);
                    if (releaseProgress >= 1f)
                    {
                        level = 0f;
                        State = AdsrState.Idle;
                        _phaseSampleIndex  = 0f;
                        _releaseStartLevel = 0f;
                    }
                    break;
            }

            CurrentLevel = Math.Clamp(level, 0f, 1f);
            return CurrentLevel;
        }
    }
}
