import keras
import tensorflow as tf
from keras.models import Model
from keras.utils import plot_model
from keras.layers import Input, Conv2D, MaxPooling2D, GlobalMaxPooling2D, BatchNormalization, SpatialDropout2D, UpSampling2D, Concatenate

def unet_classifier_network(
  loss,
  metrics,
  input_shape: tuple[int, int, int] = (256, 256, 26),
  num_classes: int = 7,
  conv_filters: int = 64,
  extra_layers: int = 2,
  kernel_size: tuple[int, int] = (3, 3),
  pool_size: tuple[int, int] = (2, 2),
  steps: int = 4,
  batch_norm: bool = True,
  lambda_l2: float | None = None,
  l_rate: float = 0.001,
  p_spatial_dropout: float | None = None,
  padding: str = 'same',
  conv_activation: str = 'elu',
  skip_connections: bool = False
):
  
    # Set the regularizer.
  if lambda_l2 is not None:
    regularizer = tf.keras.regularizers.l2(lambda_l2)
  else:
    regularizer = None

  # Store the tensor stack for skip connections.
  if skip_connections:
    tensor_stack = []

  # Input tensor along with the first two.
  input_tensor = Input(shape=input_shape, name='Input')

  # First layer.
  tensor = Conv2D(
    filters=conv_filters,
    kernel_size=kernel_size,
    padding=padding,
    kernel_regularizer=regularizer,
    activation=conv_activation,
    name="Conv_1"
  )(input_tensor)

  # Add the spatial dropout and batch norm, if either are defined.
  if p_spatial_dropout is not None:
    tensor = SpatialDropout2D(p_spatial_dropout, name="S_Dropout_1")(tensor)
  if batch_norm is not None:
    tensor = BatchNormalization(name="B_Norm_1")(tensor)

  # 2nd layer.
  tensor = Conv2D(
    filters=conv_filters * 2,
    kernel_size=kernel_size,
    padding=padding,
    kernel_regularizer=regularizer,
    activation=conv_activation,
    name="Conv_2"
  )(tensor)

  # Add the spatial dropout and batch norm, if either are defined.
  if p_spatial_dropout is not None:
    tensor = SpatialDropout2D(p_spatial_dropout, name="S_Dropout_2")(tensor)
  if batch_norm is not None:
    tensor = BatchNormalization(name="B_Norm_2")(tensor)

  # Define the sequence
  seq = [conv_filters * 2**p for p in range(1, steps + 1)]
  # steps=4, N=8: [16, 32, 64, 128]
  # tensor: (256, 256, 8)

  # Encoding (down) unet.
  for i, filter_size in enumerate(seq, start=1):
    # Append the tensor to the stack.
    if skip_connections:
      tensor_stack.append(tensor)

    # Stride for max pooling step.
    tensor = MaxPooling2D(pool_size=pool_size, strides=2, name=f"Encode_MP_{i}")(tensor)

    # I-th layer.
    tensor = Conv2D(
      filters=filter_size,
      kernel_size=kernel_size,
      padding=padding,
      kernel_regularizer=regularizer,
      activation=conv_activation,
      name=f"Encode_Conv_{i}"
    )(tensor)

    # Add the spatial dropout and batch norm, if either are defined.
    if p_spatial_dropout is not None:
      tensor = SpatialDropout2D(p_spatial_dropout, name=f"Encode_Dropout_{i}")(tensor)
    if batch_norm:
      tensor = BatchNormalization(name=f"Encode_B_Norm_{i}")(tensor)
    
    # (I-th + 1) Layer
    tensor = Conv2D(
      filters=filter_size * 2,
      kernel_size=kernel_size,
      padding=padding,
      kernel_regularizer=regularizer,
      activation=conv_activation,
      name=f"Encode_Conv_{i}_2"
    )(tensor)

    # Add the spatial dropout and batch norm, if either are defined.
    if p_spatial_dropout is not None:
      tensor = SpatialDropout2D(p_spatial_dropout, name=f"Encode_Dropout_{i}_2") (tensor)
    if batch_norm is not None:
      tensor = BatchNormalization(name=f"Encode_B_Norm_{i}_2") (tensor)

  # Decoding (up) unet.
  for i, filter_size in reversed(list(enumerate(seq, start=1))):
    # Up-sample the inital tensor.
    tensor = UpSampling2D(size=2, name=f"USample_Decode_{i}")(tensor)

    # Concat the tensor with the end of the stack (for connection)
    if skip_connections and tensor_stack:
      tensor = Concatenate(name=f"Concatenate_{i}")([tensor, tensor_stack.pop()])

    # I-th layer.
    tensor = Conv2D(
      filters=filter_size * 2,
      kernel_size=kernel_size,
      padding=padding,
      kernel_regularizer=regularizer,
      activation=conv_activation,
      name=f"Decoder_Conv_{i}"
    )(tensor)

    # Add the spatial dropout and batch norm, if either are defined.
    if p_spatial_dropout is not None:
      tensor = SpatialDropout2D(p_spatial_dropout, name=f"Decoder_S_Dropout_{i}")(tensor)
    if batch_norm is not None:
      tensor = BatchNormalization(name=f"Decoder_BNorm_{i}")(tensor)
    
    # (I-th + 1) Layer
    tensor = Conv2D(
      filters=filter_size,
      kernel_size=kernel_size,
      padding=padding,
      kernel_regularizer=regularizer,
      activation=conv_activation,
      name=f"Decoder_Conv_{i}_2"
    )(tensor)

    # Add the spatial dropout and batch norm, if either are defined.
    if p_spatial_dropout is not None:
      tensor = SpatialDropout2D(p_spatial_dropout, name=f"Decoder_S_Dropout_{i}_2")(tensor)
    if batch_norm is not None:
      tensor = BatchNormalization(name=f"Decoder_BNorm_{i}_2")(tensor)

  # Final Conditioning with the extra layers.
  for i in range(extra_layers):
    # First Extra layer.
    tensor = Conv2D(
      filters=conv_filters,
      kernel_size=kernel_size,
      padding=padding,
      kernel_regularizer=regularizer,
      activation=conv_activation,
      name=f"Conv_Last_{i+1}"
    ) (tensor)

    # Add the spatial dropout and batch norm, if either are defined.
    if p_spatial_dropout is not None:
      tensor = SpatialDropout2D(p_spatial_dropout, name=f"S_Dropout_Last_{i+1}") (tensor)
    if batch_norm is not None:
      tensor = BatchNormalization(name=f"B_Norm_Last_{i+1}") (tensor)

  # Output tensor.
  tensor = Conv2D(
    filters=conv_filters,
    kernel_size=kernel_size,
    padding=padding,
    activation="softmax",
    name="Output"
  )(tensor)

  # Add the optimizer.
  opt = keras.optimizers.Adam(learning_rate=l_rate, amsgrad=False)

  # Define the model.
  model = Model(inputs=input_tensor, outputs=tensor)
  model.compile(loss=loss, optimizer=opt, metrics=metrics)

  # Return the model.
  return model