SELECT checkpoint
FROM __schema__.projection_checkpoints
WHERE checkpoint_name = @CheckpointName