/**
 * StoneHammer v21.1: Audio Module (Synth Edition)
 * Uses Web Audio API to generate retro SFX on the fly.
 * No external files required.
 */
(function () {
    const sh = window.stoneHammer = window.stoneHammer || {};

    sh.audio = {
        enabled: true,
        volume: 0.3,
        ctx: null,

        init: function () {
            try {
                const AudioContext = window.AudioContext || window.webkitAudioContext;
                this.ctx = new AudioContext();
                console.log("[Audio] Synth Engine Initialized");
            } catch (e) {
                console.error("[Audio] Use Chrome/Firefox.", e);
            }
        },

        resume: function () {
            if (this.ctx && this.ctx.state === 'suspended') {
                this.ctx.resume();
            }
        },

        playSound: function (id) {
            if (!this.enabled || !this.ctx) return;
            this.resume();

            // Simple Retro Synth Mapping
            switch (id) {
                case "attack_melee": this.playTone(100, "square", 0.1, -50); break;   // "Bap"
                case "attack_range": this.playTone(600, "sawtooth", 0.1, -300); break; // "Pew"
                case "spell_fire": this.playTone(150, "sawtooth", 0.4, -50); break; // "Vwoom"
                case "impact_flesh": this.playNoise(0.05); break;                     // "Tic"
                case "ui_click": this.playTone(800, "sine", 0.05, 0); break;
                default: this.playTone(440, "sine", 0.1, 0); break;       // Fallback Beep
            }
        },

        playTone: function (freq, type, dur, slide) {
            try {
                const osc = this.ctx.createOscillator();
                const gain = this.ctx.createGain();

                osc.type = type;
                osc.frequency.setValueAtTime(freq, this.ctx.currentTime);
                if (slide !== 0) {
                    osc.frequency.linearRampToValueAtTime(Math.max(1, freq + slide), this.ctx.currentTime + dur);
                }

                gain.gain.setValueAtTime(this.volume, this.ctx.currentTime);
                gain.gain.exponentialRampToValueAtTime(0.01, this.ctx.currentTime + dur);

                osc.connect(gain);
                gain.connect(this.ctx.destination);

                osc.start();
                osc.stop(this.ctx.currentTime + dur);
            } catch (e) { console.warn("[Audio] Tone Error", e); }
        },

        playNoise: function (dur) {
            try {
                const bufferSize = this.ctx.sampleRate * dur;
                const buffer = this.ctx.createBuffer(1, bufferSize, this.ctx.sampleRate);
                const data = buffer.getChannelData(0);

                for (let i = 0; i < bufferSize; i++) {
                    data[i] = Math.random() * 2 - 1;
                }

                const noise = this.ctx.createBufferSource();
                noise.buffer = buffer;

                const gain = this.ctx.createGain();
                // Ensure positive start value for exponential ramp
                const startVol = Math.max(0.001, this.volume * 0.5);
                gain.gain.setValueAtTime(startVol, this.ctx.currentTime);
                gain.gain.exponentialRampToValueAtTime(0.01, this.ctx.currentTime + dur);

                noise.connect(gain);
                gain.connect(this.ctx.destination);
                noise.start();
            } catch (e) { console.warn("[Audio] Noise Error", e); }
        },

        setVolume: function (vol) {
            this.volume = Math.max(0, Math.min(1, vol));
        }
    };

    // Unlock AudioContext on interaction
    window.addEventListener('click', () => sh.audio.resume(), { once: true });
    window.addEventListener('keydown', () => sh.audio.resume(), { once: true });

    // Auto-init
    sh.audio.init();
    console.log("StoneHammer: Audio Module (Synth) Loaded");
})();
