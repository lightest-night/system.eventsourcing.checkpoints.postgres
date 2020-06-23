CREATE TABLE IF NOT EXISTS __schema__.projection_checkpoints (
    id                  BIGINT          GENERATED ALWAYS AS IDENTITY    NOT NULL,
    checkpoint_name     VARCHAR(500)                                    NOT NULL,
    checkpoint          BIGINT                                          NULL,
    CONSTRAINT pk_projection_checkpoints PRIMARY KEY (id),
    CONSTRAINT uq_projection_checkpoints_checkpoint_name UNIQUE (checkpoint_name)
);

CREATE INDEX IF NOT EXISTS ix_projection_checkpoints_checkpoint_name
    ON __schema__.projection_checkpoints (checkpoint_name);