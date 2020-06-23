INSERT INTO __schema__.projection_checkpoints (checkpoint_name, checkpoint)
VALUES (@CheckpointName, @Checkpoint)
ON CONFLICT (checkpoint_name)
DO UPDATE SET checkpoint = @Checkpoint