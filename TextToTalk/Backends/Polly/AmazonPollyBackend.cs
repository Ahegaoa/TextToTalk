﻿using AdysTech.CredentialManager;
using Amazon;
using Amazon.Polly;
using Amazon.Polly.Model;
using ImGuiNET;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Numerics;
using Gender = TextToTalk.GameEnums.Gender;

namespace TextToTalk.Backends.Polly
{
    public class AmazonPollyBackend : VoiceBackend
    {
        private const string CredentialsTarget = "TextToTalk_AccessKeys_AmazonPolly";

        private static readonly Vector4 HintColor = new(0.7f, 0.7f, 0.7f, 1.0f);
        private static readonly Vector4 Red = new(1, 0, 0, 1);

        private static readonly string[] Regions = RegionEndpoint.EnumerableAllRegions.Select(r => r.SystemName).ToArray();
        private static readonly string[] Engines = { Engine.Neural, Engine.Standard };

        private readonly PluginConfiguration config;

        private PollyClient polly;
        private IList<Voice> voices;

        private string accessKey = string.Empty;
        private string secretKey = string.Empty;

        public AmazonPollyBackend(PluginConfiguration config)
        {
            TitleBarColor = ImGui.ColorConvertU32ToFloat4(0xFF0099FF);

            var credentials = CredentialManager.GetCredentials(CredentialsTarget);

            if (credentials != null)
            {
                this.accessKey = credentials.UserName;
                this.secretKey = credentials.Password;
                this.polly = new PollyClient(credentials.UserName, credentials.Password, RegionEndpoint.EUWest1);
                this.voices = this.polly.GetVoicesForEngine(this.config.PollyEngine);
            }
            else
            {
                this.voices = new List<Voice>();
            }

            this.config = config;
        }

        public override void Say(Gender gender, string text)
        {
            var voiceIdStr = gender switch
            {
                Gender.Male => this.config.PollyVoiceMale,
                Gender.Female => this.config.PollyVoiceFemale,
                _ => this.config.PollyVoice,
            };

            var voiceId = this.voices
                .Select(v => v.Id)
                .FirstOrDefault(id => id == voiceIdStr) ?? VoiceId.Matthew;

            _ = this.polly.Say(voiceId, text);
        }

        public override void CancelSay()
        {
            _ = this.polly.Cancel();
        }

        public override void DrawSettings(ImExposedFunctions helpers)
        {
            var region = this.config.PollyRegion;
            var regionIndex = Array.IndexOf(Regions, region);
            if (ImGui.Combo("Region##TTTPollyRegion", ref regionIndex, Regions, Regions.Length))
            {
                this.config.PollyRegion = Regions[regionIndex];
                this.config.Save();
            }

            ImGui.InputTextWithHint("##TTTPollyAccessKey", "Access key", ref this.accessKey, 100, ImGuiInputTextFlags.Password);
            ImGui.InputTextWithHint("##TTTPollySecretKey", "Secret key", ref this.secretKey, 100, ImGuiInputTextFlags.Password);

            if (ImGui.Button("Save##TTTSavePollyAuth"))
            {
                var credentials = new NetworkCredential(this.accessKey, this.secretKey);
                CredentialManager.SaveCredentials(CredentialsTarget, credentials);

                this.polly?.Dispose();
                this.polly = new PollyClient(this.accessKey, this.secretKey, RegionEndpoint.EUWest1);
            }

            ImGui.TextColored(HintColor, "Credentials secured with Windows Credential Manager");

            var engine = this.config.PollyEngine;
            var engineIndex = Array.IndexOf(Engines, engine);
            if (ImGui.Combo("Engine##TTTPollyEngine", ref engineIndex, Engines, Engines.Length))
            {
                this.config.PollyEngine = Engines[engineIndex];
                this.config.Save();

                this.voices = this.polly.GetVoicesForEngine(this.config.PollyEngine);
            }

            var voiceArray = this.voices.Select(v => v.Name).ToArray();
            var voiceIdArray = this.voices.Select(v => v.Id).ToArray();

            var currentVoiceId = this.config.PollyVoice;

            var voiceIndex = Array.IndexOf(voiceIdArray, currentVoiceId);
            if (ImGui.Combo("Voice##TTTVoice1", ref voiceIndex, voiceArray, this.voices.Count))
            {
                this.config.PollyVoice = voiceIdArray[voiceIndex];
                this.config.Save();
            }

            if (this.voices.FirstOrDefault(v => v.Id == this.config.PollyVoice) == null)
            {
                ImGui.TextColored(Red, "Voice not supported on this engine");
            }

            var useGenderedVoicePresets = this.config.UseGenderedVoicePresets;
            if (ImGui.Checkbox("Use gendered voices##TTTVoice2", ref useGenderedVoicePresets))
            {
                this.config.UseGenderedVoicePresets = useGenderedVoicePresets;
                this.config.Save();
            }

            if (useGenderedVoicePresets)
            {
                var currentMaleVoiceId = this.config.PollyVoiceMale;
                var currentFemaleVoiceId = this.config.PollyVoiceFemale;

                var maleVoiceIndex = Array.IndexOf(voiceIdArray, currentMaleVoiceId);
                if (ImGui.Combo("Male voice##TTTVoice3", ref maleVoiceIndex, voiceArray, this.voices.Count))
                {
                    this.config.PollyVoiceMale = voiceIdArray[maleVoiceIndex];
                    this.config.Save();
                }

                if (this.voices.FirstOrDefault(v => v.Id == this.config.PollyVoiceMale) == null)
                {
                    ImGui.TextColored(Red, "Voice not supported on this engine");
                }

                var femaleVoiceIndex = Array.IndexOf(voiceIdArray, currentFemaleVoiceId);
                if (ImGui.Combo("Female voice##TTTVoice4", ref femaleVoiceIndex, voiceArray, this.voices.Count))
                {
                    this.config.PollyVoiceFemale = voiceIdArray[femaleVoiceIndex];
                    this.config.Save();
                }

                if (this.voices.FirstOrDefault(v => v.Id == this.config.PollyVoiceFemale) == null)
                {
                    ImGui.TextColored(Red, "Voice not supported on this engine");
                }
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                this.polly?.Dispose();
            }
        }
    }
}