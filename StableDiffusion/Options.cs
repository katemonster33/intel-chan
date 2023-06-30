using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace IntelChan.StableDiffusion
{
    public class Options
    {
        [JsonPropertyName("samples_save")]
        public bool? SamplesSave { get; set; }

        [JsonPropertyName("samples_format")]
        public string? SamplesFormat { get; set; }

        [JsonPropertyName("samples_filename_pattern")]
        public string? SamplesFilenamePattern { get; set; }

        [JsonPropertyName("save_images_add_number")]
        public bool? SaveImagesAddNumber { get; set; }

        [JsonPropertyName("grid_save")]
        public bool? GridSave { get; set; }

        [JsonPropertyName("grid_format")]
        public string? GridFormat { get; set; }

        [JsonPropertyName("grid_extended_filename")]
        public bool? GridExtendedFilename { get; set; }

        [JsonPropertyName("grid_only_if_multiple")]
        public bool? GridOnlyIfMultiple { get; set; }

        [JsonPropertyName("grid_prevent_empty_spots")]
        public bool? GridPreventEmptySpots { get; set; }

        [JsonPropertyName("n_rows")]
        public long? NRows { get; set; }

        [JsonPropertyName("enable_pnginfo")]
        public bool? EnablePnginfo { get; set; }

        [JsonPropertyName("save_txt")]
        public bool? SaveTxt { get; set; }

        [JsonPropertyName("save_images_before_face_restoration")]
        public bool? SaveImagesBeforeFaceRestoration { get; set; }

        [JsonPropertyName("save_images_before_highres_fix")]
        public bool? SaveImagesBeforeHighresFix { get; set; }

        [JsonPropertyName("save_images_before_color_correction")]
        public bool? SaveImagesBeforeColorCorrection { get; set; }

        [JsonPropertyName("save_mask")]
        public bool? SaveMask { get; set; }

        [JsonPropertyName("save_mask_composite")]
        public bool? SaveMaskComposite { get; set; }

        [JsonPropertyName("jpeg_quality")]
        public long? JpegQuality { get; set; }

        [JsonPropertyName("webp_lossless")]
        public bool? WebpLossless { get; set; }

        [JsonPropertyName("export_for_4chan")]
        public bool? ExportFor4Chan { get; set; }

        [JsonPropertyName("img_downscale_threshold")]
        public long? ImgDownscaleThreshold { get; set; }

        [JsonPropertyName("target_side_length")]
        public long? TargetSideLength { get; set; }

        [JsonPropertyName("img_max_size_mp")]
        public long? ImgMaxSizeMp { get; set; }

        [JsonPropertyName("use_original_name_batch")]
        public bool? UseOriginalNameBatch { get; set; }

        [JsonPropertyName("use_upscaler_name_as_suffix")]
        public bool? UseUpscalerNameAsSuffix { get; set; }

        [JsonPropertyName("save_selected_only")]
        public bool? SaveSelectedOnly { get; set; }

        [JsonPropertyName("save_init_img")]
        public bool? SaveInitImg { get; set; }

        [JsonPropertyName("temp_dir")]
        public string? TempDir { get; set; }

        [JsonPropertyName("clean_temp_dir_at_start")]
        public bool? CleanTempDirAtStart { get; set; }

        [JsonPropertyName("outdir_samples")]
        public string? OutdirSamples { get; set; }

        [JsonPropertyName("outdir_txt2img_samples")]
        public string? OutdirTxt2ImgSamples { get; set; }

        [JsonPropertyName("outdir_img2img_samples")]
        public string? OutdirImg2ImgSamples { get; set; }

        [JsonPropertyName("outdir_extras_samples")]
        public string? OutdirExtrasSamples { get; set; }

        [JsonPropertyName("outdir_grids")]
        public string? OutdirGrids { get; set; }

        [JsonPropertyName("outdir_txt2img_grids")]
        public string? OutdirTxt2ImgGrids { get; set; }

        [JsonPropertyName("outdir_img2img_grids")]
        public string? OutdirImg2ImgGrids { get; set; }

        [JsonPropertyName("outdir_save")]
        public string? OutdirSave { get; set; }

        [JsonPropertyName("outdir_init_images")]
        public string? OutdirInitImages { get; set; }

        [JsonPropertyName("save_to_dirs")]
        public bool? SaveToDirs { get; set; }

        [JsonPropertyName("grid_save_to_dirs")]
        public bool? GridSaveToDirs { get; set; }

        [JsonPropertyName("use_save_to_dirs_for_ui")]
        public bool? UseSaveToDirsForUi { get; set; }

        [JsonPropertyName("directories_filename_pattern")]
        public string? DirectoriesFilenamePattern { get; set; }

        [JsonPropertyName("directories_max_prompt_words")]
        public long? DirectoriesMaxPromptWords { get; set; }

        [JsonPropertyName("ESRGAN_tile")]
        public long? EsrganTile { get; set; }

        [JsonPropertyName("ESRGAN_tile_overlap")]
        public long? EsrganTileOverlap { get; set; }

        [JsonPropertyName("realesrgan_enabled_models")]
        public List<string>? RealesrganEnabledModels { get; set; }

        [JsonPropertyName("upscaler_for_img2img")]
        public string? UpscalerForImg2Img { get; set; }

        [JsonPropertyName("face_restoration_model")]
        public string? FaceRestorationModel { get; set; }

        [JsonPropertyName("code_former_weight")]
        public double? CodeFormerWeight { get; set; }

        [JsonPropertyName("face_restoration_unload")]
        public bool? FaceRestorationUnload { get; set; }

        [JsonPropertyName("show_warnings")]
        public bool? ShowWarnings { get; set; }

        [JsonPropertyName("memmon_poll_rate")]
        public long? MemmonPollRate { get; set; }

        [JsonPropertyName("samples_log_stdout")]
        public bool? SamplesLogStdout { get; set; }

        [JsonPropertyName("multiple_tqdm")]
        public bool? MultipleTqdm { get; set; }

        [JsonPropertyName("print_hypernet_extra")]
        public bool? PrintHypernetExtra { get; set; }

        [JsonPropertyName("list_hidden_files")]
        public bool? ListHiddenFiles { get; set; }

        [JsonPropertyName("unload_models_when_training")]
        public bool? UnloadModelsWhenTraining { get; set; }

        [JsonPropertyName("pin_memory")]
        public bool? PinMemory { get; set; }

        [JsonPropertyName("save_optimizer_state")]
        public bool? SaveOptimizerState { get; set; }

        [JsonPropertyName("save_training_settings_to_txt")]
        public bool? SaveTrainingSettingsToTxt { get; set; }

        [JsonPropertyName("dataset_filename_word_regex")]
        public string? DatasetFilenameWordRegex { get; set; }

        [JsonPropertyName("dataset_filename_join_string")]
        public string? DatasetFilenameJoinstring { get; set; }

        [JsonPropertyName("training_image_repeats_per_epoch")]
        public long? TrainingImageRepeatsPerEpoch { get; set; }

        [JsonPropertyName("training_write_csv_every")]
        public long? TrainingWriteCsvEvery { get; set; }

        [JsonPropertyName("training_xattention_optimizations")]
        public bool? TrainingXattentionOptimizations { get; set; }

        [JsonPropertyName("training_enable_tensorboard")]
        public bool? TrainingEnableTensorboard { get; set; }

        [JsonPropertyName("training_tensorboard_save_images")]
        public bool? TrainingTensorboardSaveImages { get; set; }

        [JsonPropertyName("training_tensorboard_flush_every")]
        public long? TrainingTensorboardFlushEvery { get; set; }

        [JsonPropertyName("sd_model_checkpoint")]
        public string? SdModelCheckpoint { get; set; }

        [JsonPropertyName("sd_checkpoint_cache")]
        public long? SdCheckpointCache { get; set; }

        [JsonPropertyName("sd_vae_checkpoint_cache")]
        public long? SdVaeCheckpointCache { get; set; }

        [JsonPropertyName("sd_vae")]
        public string? SdVae { get; set; }

        [JsonPropertyName("sd_vae_as_default")]
        public bool? SdVaeAsDefault { get; set; }

        [JsonPropertyName("inpainting_mask_weight")]
        public long? InpaintingMaskWeight { get; set; }

        [JsonPropertyName("initial_noise_multiplier")]
        public long? InitialNoiseMultiplier { get; set; }

        [JsonPropertyName("img2img_color_correction")]
        public bool? Img2ImgColorCorrection { get; set; }

        [JsonPropertyName("img2img_fix_steps")]
        public bool? Img2ImgFixSteps { get; set; }

        [JsonPropertyName("img2img_background_color")]
        public string? Img2ImgBackgroundColor { get; set; }

        [JsonPropertyName("enable_quantization")]
        public bool? EnableQuantization { get; set; }

        [JsonPropertyName("enable_emphasis")]
        public bool? EnableEmphasis { get; set; }

        [JsonPropertyName("enable_batch_seeds")]
        public bool? EnableBatchSeeds { get; set; }

        [JsonPropertyName("comma_padding_backtrack")]
        public long? CommaPaddingBacktrack { get; set; }

        [JsonPropertyName("CLIP_stop_at_last_layers")]
        public long? ClipStopAtLastLayers { get; set; }

        [JsonPropertyName("upcast_attn")]
        public bool? UpcastAttn { get; set; }

        [JsonPropertyName("randn_source")]
        public string? RandnSource { get; set; }

        [JsonPropertyName("cross_attention_optimization")]
        public string? CrossAttentionOptimization { get; set; }

        [JsonPropertyName("s_min_uncond")]
        public long? SMinUncond { get; set; }

        [JsonPropertyName("token_merging_ratio")]
        public long? TokenMergingRatio { get; set; }

        [JsonPropertyName("token_merging_ratio_img2img")]
        public long? TokenMergingRatioImg2Img { get; set; }

        [JsonPropertyName("token_merging_ratio_hr")]
        public long? TokenMergingRatioHr { get; set; }

        [JsonPropertyName("use_old_emphasis_implementation")]
        public bool? UseOldEmphasisImplementation { get; set; }

        [JsonPropertyName("use_old_karras_scheduler_sigmas")]
        public bool? UseOldKarrasSchedulerSigmas { get; set; }

        [JsonPropertyName("no_dpmpp_sde_batch_determinism")]
        public bool? NoDpmppSdeBatchDeterminism { get; set; }

        [JsonPropertyName("use_old_hires_fix_width_height")]
        public bool? UseOldHiresFixWidthHeight { get; set; }

        [JsonPropertyName("dont_fix_second_order_samplers_schedule")]
        public bool? DontFixSecondOrderSamplersSchedule { get; set; }

        [JsonPropertyName("interrogate_keep_models_in_memory")]
        public bool? InterrogateKeepModelsInMemory { get; set; }

        [JsonPropertyName("interrogate_return_ranks")]
        public bool? InterrogateReturnRanks { get; set; }

        [JsonPropertyName("interrogate_clip_num_beams")]
        public long? InterrogateClipNumBeams { get; set; }

        [JsonPropertyName("interrogate_clip_min_length")]
        public long? InterrogateClipMinLength { get; set; }

        [JsonPropertyName("interrogate_clip_max_length")]
        public long? InterrogateClipMaxLength { get; set; }

        [JsonPropertyName("interrogate_clip_dict_limit")]
        public long? InterrogateClipDictLimit { get; set; }

        [JsonPropertyName("interrogate_clip_skip_categories")]
        public List<object>? InterrogateClipSkipCategories { get; set; }

        [JsonPropertyName("interrogate_deepbooru_score_threshold")]
        public double? InterrogateDeepbooruScoreThreshold { get; set; }

        [JsonPropertyName("deepbooru_sort_alpha")]
        public bool? DeepbooruSortAlpha { get; set; }

        [JsonPropertyName("deepbooru_use_spaces")]
        public bool? DeepbooruUseSpaces { get; set; }

        [JsonPropertyName("deepbooru_escape")]
        public bool? DeepbooruEscape { get; set; }

        [JsonPropertyName("deepbooru_filter_tags")]
        public string? DeepbooruFilterTags { get; set; }

        [JsonPropertyName("extra_networks_show_hidden_directories")]
        public bool? ExtraNetworksShowHiddenDirectories { get; set; }

        [JsonPropertyName("extra_networks_hidden_models")]
        public string? ExtraNetworksHiddenModels { get; set; }

        [JsonPropertyName("extra_networks_default_view")]
        public string? ExtraNetworksDefaultView { get; set; }

        [JsonPropertyName("extra_networks_default_multiplier")]
        public long? ExtraNetworksDefaultMultiplier { get; set; }

        [JsonPropertyName("extra_networks_card_width")]
        public long? ExtraNetworksCardWidth { get; set; }

        [JsonPropertyName("extra_networks_card_height")]
        public long? ExtraNetworksCardHeight { get; set; }

        [JsonPropertyName("extra_networks_add_text_separator")]
        public string? ExtraNetworksAddTextSeparator { get; set; }

        [JsonPropertyName("ui_extra_networks_tab_reorder")]
        public string? UiExtraNetworksTabReorder { get; set; }

        [JsonPropertyName("sd_hypernetwork")]
        public string? SdHypernetwork { get; set; }

        [JsonPropertyName("localization")]
        public string? Localization { get; set; }

        [JsonPropertyName("gradio_theme")]
        public string? GradioTheme { get; set; }

        [JsonPropertyName("img2img_editor_height")]
        public long? Img2ImgEditorHeight { get; set; }

        [JsonPropertyName("return_grid")]
        public bool? ReturnGrid { get; set; }

        [JsonPropertyName("return_mask")]
        public bool? ReturnMask { get; set; }

        [JsonPropertyName("return_mask_composite")]
        public bool? ReturnMaskComposite { get; set; }

        [JsonPropertyName("do_not_show_images")]
        public bool? DoNotShowImages { get; set; }

        [JsonPropertyName("send_seed")]
        public bool? SendSeed { get; set; }

        [JsonPropertyName("send_size")]
        public bool? SendSize { get; set; }

        [JsonPropertyName("font")]
        public string? Font { get; set; }

        [JsonPropertyName("js_modal_lightbox")]
        public bool? JsModalLightbox { get; set; }

        [JsonPropertyName("js_modal_lightbox_initially_zoomed")]
        public bool? JsModalLightboxInitiallyZoomed { get; set; }

        [JsonPropertyName("js_modal_lightbox_gamepad")]
        public bool? JsModalLightboxGamepad { get; set; }

        [JsonPropertyName("js_modal_lightbox_gamepad_repeat")]
        public long? JsModalLightboxGamepadRepeat { get; set; }

        [JsonPropertyName("show_progress_in_title")]
        public bool? ShowProgressInTitle { get; set; }

        [JsonPropertyName("samplers_in_dropdown")]
        public bool? SamplersInDropdown { get; set; }

        [JsonPropertyName("dimensions_and_batch_together")]
        public bool? DimensionsAndBatchTogether { get; set; }

        [JsonPropertyName("keyedit_precision_attention")]
        public double? KeyeditPrecisionAttention { get; set; }

        [JsonPropertyName("keyedit_precision_extra")]
        public double? KeyeditPrecisionExtra { get; set; }

        [JsonPropertyName("keyedit_delimiters")]
        public string? KeyeditDelimiters { get; set; }

        [JsonPropertyName("quicksettings_list")]
        public List<string>? QuicksettingsList { get; set; }

        [JsonPropertyName("ui_tab_order")]
        public List<object>? UiTabOrder { get; set; }

        [JsonPropertyName("hidden_tabs")]
        public List<object>? HiddenTabs { get; set; }

        [JsonPropertyName("ui_reorder")]
        public string? UiReorder { get; set; }

        [JsonPropertyName("hires_fix_show_sampler")]
        public bool? HiresFixShowSampler { get; set; }

        [JsonPropertyName("hires_fix_show_prompts")]
        public bool? HiresFixShowPrompts { get; set; }

        [JsonPropertyName("add_model_hash_to_info")]
        public bool? AddModelHashToInfo { get; set; }

        [JsonPropertyName("add_model_name_to_info")]
        public bool? AddModelNameToInfo { get; set; }

        [JsonPropertyName("add_version_to_infotext")]
        public bool? AddVersionToInfotext { get; set; }

        [JsonPropertyName("disable_weights_auto_swap")]
        public bool? DisableWeightsAutoSwap { get; set; }

        [JsonPropertyName("show_progressbar")]
        public bool? ShowProgressbar { get; set; }

        [JsonPropertyName("live_previews_enable")]
        public bool? LivePreviewsEnable { get; set; }

        [JsonPropertyName("live_previews_image_format")]
        public string? LivePreviewsImageFormat { get; set; }

        [JsonPropertyName("show_progress_grid")]
        public bool? ShowProgressGrid { get; set; }

        [JsonPropertyName("show_progress_every_n_steps")]
        public long? ShowProgressEveryNSteps { get; set; }

        [JsonPropertyName("show_progress_type")]
        public string? ShowProgressType { get; set; }

        [JsonPropertyName("live_preview_content")]
        public string? LivePreviewContent { get; set; }

        [JsonPropertyName("live_preview_refresh_period")]
        public long? LivePreviewRefreshPeriod { get; set; }

        [JsonPropertyName("hide_samplers")]
        public List<object>? HideSamplers { get; set; }

        [JsonPropertyName("eta_ddim")]
        public long? EtaDdim { get; set; }

        [JsonPropertyName("eta_ancestral")]
        public long? EtaAncestral { get; set; }

        [JsonPropertyName("ddim_discretize")]
        public string? DdimDiscretize { get; set; }

        [JsonPropertyName("s_churn")]
        public long? SChurn { get; set; }

        [JsonPropertyName("s_tmin")]
        public long? STmin { get; set; }

        [JsonPropertyName("s_noise")]
        public long? SNoise { get; set; }

        [JsonPropertyName("eta_noise_seed_delta")]
        public long? EtaNoiseSeedDelta { get; set; }

        [JsonPropertyName("always_discard_next_to_last_sigma")]
        public bool? AlwaysDiscardNextToLastSigma { get; set; }

        [JsonPropertyName("uni_pc_variant")]
        public string? UniPcVariant { get; set; }

        [JsonPropertyName("uni_pc_skip_type")]
        public string? UniPcSkipType { get; set; }

        [JsonPropertyName("uni_pc_order")]
        public long? UniPcOrder { get; set; }

        [JsonPropertyName("uni_pc_lower_order_final")]
        public bool? UniPcLowerOrderFinal { get; set; }

        [JsonPropertyName("postprocessing_enable_in_main_ui")]
        public List<string>? PostprocessingEnableInMainUi { get; set; }

        [JsonPropertyName("postprocessing_operation_order")]
        public List<string>? PostprocessingOperationOrder { get; set; }

        [JsonPropertyName("upscaling_max_images_in_cache")]
        public long? UpscalingMaxImagesInCache { get; set; }

        [JsonPropertyName("disabled_extensions")]
        public List<object>? DisabledExtensions { get; set; }

        [JsonPropertyName("disable_all_extensions")]
        public string? DisableAllExtensions { get; set; }

        [JsonPropertyName("restore_config_state_file")]
        public string? RestoreConfigStateFile { get; set; }

        [JsonPropertyName("sd_checkpoint_hash")]
        public string? SdCheckpointHash { get; set; }

        [JsonPropertyName("sd_lora")]
        public string? SdLora { get; set; }

        [JsonPropertyName("lora_preferred_name")]
        public string? LoraPreferredName { get; set; }

        [JsonPropertyName("lora_add_hashes_to_infotext")]
        public bool? LoraAddHashesToInfotext { get; set; }

        [JsonPropertyName("lora_functional")]
        public bool? LoraFunctional { get; set; }
    }
}
