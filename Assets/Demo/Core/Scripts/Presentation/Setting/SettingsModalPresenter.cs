using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using Demo.Core.Scripts.Foundation.Common;
using Demo.Core.Scripts.Presentation.LockConfirmation;
using Demo.Core.Scripts.UseCase.Setting;
using Demo.Core.Scripts.View.Setting;
using Demo.Subsystem.Misc;
using R3;
using UnityScreenNavigator;

namespace Demo.Core.Scripts.Presentation.Setting
{
    [AssetAddress(ResourceKey.Prefabs.SettingsModal)]
    public sealed class SettingsModalPresenter : IPresenter, ILifecycleHandler, IDisposableCollectionHolder
    {
        private readonly SettingsModal view;
        private readonly IScreenNavigator screenNavigator;
        private readonly SettingsUseCase useCase;
        private readonly List<IDisposable> disposables = new();
        private SettingsViewState viewState;
        private bool dirty;

        public SettingsModalPresenter(SettingsModal view, IScreenNavigator screenNavigator, SettingsUseCase useCase)
        {
            this.view = view;
            this.screenNavigator = screenNavigator;
            this.useCase = useCase;
        }

        ICollection<IDisposable> IDisposableCollectionHolder.GetDisposableCollection() => disposables;

        public async UniTask InitializeAsync()
        {
            viewState = new SettingsViewState();
            disposables.Add(viewState);

            // Update models.
            await useCase.FetchSoundSettingsAsync();
            var model = useCase.Model;

            // Set view state with initial values.
            SetVoiceSettingsViewState(model.Sounds.Voice.Volume, model.Sounds.Voice.Muted);
            SetBgmSettingsViewState(model.Sounds.Bgm.Volume, model.Sounds.Bgm.Muted);
            SetSeSettingsViewState(model.Sounds.Se.Volume, model.Sounds.Se.Muted);

            // Observe changes of models.
            model.Sounds.Voice
                .ValueChanged
                .Subscribe(x => SetVoiceSettingsViewState(x.Volume, x.Muted))
                .AddTo(this);
            model.Sounds.Bgm
                .ValueChanged
                .Subscribe(x => SetBgmSettingsViewState(x.Volume, x.Muted))
                .AddTo(this);
            model.Sounds.Se
                .ValueChanged
                .Subscribe(x => SetSeSettingsViewState(x.Volume, x.Muted))
                .AddTo(this);

            // Observe changes of view state.
            viewState.SoundSettings.IsVoiceEnabled.Subscribe(_ => dirty = true).AddTo(this);
            viewState.SoundSettings.IsBgmEnabled.Subscribe(_ => dirty = true).AddTo(this);
            viewState.SoundSettings.IsSeEnabled.Subscribe(_ => dirty = true).AddTo(this);
            viewState.SoundSettings.VoiceVolume.Subscribe(_ => dirty = true).AddTo(this);
            viewState.SoundSettings.SeVolume.Subscribe(_ => dirty = true).AddTo(this);
            viewState.SoundSettings.BgmVolume.Subscribe(_ => dirty = true).AddTo(this);
            viewState.CloseButtonClicked
                .Subscribe(_ => screenNavigator.PopModalAsync(this).Forget())
                .AddTo(this);
            viewState.LockedButtonClicked
                .Subscribe(_ => screenNavigator.PushModalAsync<LockConfirmationModalPresenter>().Forget())
                .AddTo(this);

            await view.root.InitializeAsync(viewState);
        }

        private void SetVoiceSettingsViewState(float volume, bool isMuted)
        {
            viewState.SoundSettings.VoiceVolume.Value = volume;
            viewState.SoundSettings.IsVoiceEnabled.Value = !isMuted;
        }

        private void SetBgmSettingsViewState(float volume, bool isMuted)
        {
            viewState.SoundSettings.BgmVolume.Value = volume;
            viewState.SoundSettings.IsBgmEnabled.Value = !isMuted;
        }

        private void SetSeSettingsViewState(float volume, bool isMuted)
        {
            viewState.SoundSettings.SeVolume.Value = volume;
            viewState.SoundSettings.IsSeEnabled.Value = !isMuted;
        }

        public UniTask WillPushExitAsync() => SaveIfDirtyAsync();

        public UniTask WillPopExitAsync() => SaveIfDirtyAsync();

        private async UniTask SaveIfDirtyAsync()
        {
            if (!dirty)
                return;

            await useCase.SaveSoundSettingsAsync
            (
                new SettingsUseCase.SaveSoundSettingsRequest(
                    viewState.SoundSettings.VoiceVolume.Value,
                    viewState.SoundSettings.BgmVolume.Value,
                    viewState.SoundSettings.SeVolume.Value,
                    !viewState.SoundSettings.IsVoiceEnabled.Value,
                    !viewState.SoundSettings.IsBgmEnabled.Value,
                    !viewState.SoundSettings.IsSeEnabled.Value
                )
            );
        }

        public void Dispose()
        {
            foreach (var disposable in disposables)
                disposable.Dispose();
        }
    }
}
