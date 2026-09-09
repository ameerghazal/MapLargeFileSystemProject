import pickle
import matplotlib.pyplot as plt
import numpy as np
import tensorflow as tf
from tensorflow.keras.utils import plot_model
from sklearn.metrics import confusion_matrix, ConfusionMatrixDisplay
import keras
from chesapeake_loader4 import create_datasets, create_single_dataset

def figure1(shallow_model, deep_model):
  """
  Generate model architecture diagrams (Figure 1a,b).
    
  :param shallow_model: keras.Model
  :param deep_model: keras.Model
  :param save_path: (Directory to save the figures)
  """

  # Figure 1a: shallow model.
  plt.figure(figsize=(10, 12))
  plot_model(
    shallow_model,
    to_file='shallow_model.png',
    show_shapes=True,
    show_layer_names=True
  )
  plt.imshow('shallow_model.png')
  plt.title('Figure 1a: Shallow Model Architecture', fontsize=14)
  plt.axis("off")
  plt.savefig(
    f"imgs/figure_1a_shallow_architecture.png"
  )

  # Figure 1b: deep model.
  plt.figure(figsize=(10, 12))
  plot_model(
    deep_model,
    to_file='deep_model.png',
    show_shapes=True,
    show_layer_names=True
  )
  plt.imshow('deep_model.png')
  plt.title('Figure 1b: Deep Model Architecture', fontsize=14)
  plt.axis("off")
  plt.savefig(
    f"imgs/figure_1b_deep_architecture.png"
  )

def figure2(shallow_data, deep_data):
  '''
  Learning curves (validation accuracy function of epoch) for the shallow and deep models. 5 curves per model.
  '''
  # Plot figure 2a: validation accuracy vs. epochs for shallow.
  fig = plt.figure()
  for i in range(5):
    # Plot the shallow data (Grab the accuracy, 2nd param.).
    plt.plot(
      shallow_data[i]['history']['val_sparse_categorical_accuracy'],
      label=f"Shallow_{i}",
      alpha=0.6
    )
  
  plt.ylabel("Validation Accuracy")
  plt.xlabel("Epoch")
  plt.legend()
  plt.title("Figure 2a: Validation accuracy vs. epochs (shallow)")
  plt.savefig('imgs/figure_2a')

  # Plot figure 2b: validation accuracy vs. epochs for deep.
  fig = plt.figure()
  for i in range(5):
    # Plot the deep data.
    plt.plot(
      deep_data[i]['history']['val_sparse_categorical_accuracy'],
      label=f"Deep_{i}",
      alpha=0.6
    )
  
  plt.ylabel("Validation Accuracy")
  plt.xlabel("Epoch")
  plt.legend()
  plt.title("Figure 2b: Validation accuracy vs. epochs (deep)")
  plt.savefig('imgs/figure_2b')

def figure3(shallow_data, deep_data, shallow_models, deep_models, classes):
  '''
  Confusion matrix, combining test data across all 5 folds for each model.
  '''

  # Compare true labels and predictions across all folds.
  for i in range(5):
    # Extract the args.
    args = shallow_data[i]['args']

    # Load dataset and corresponding models
    _, _, ds_testing, _ = create_datasets(base_dir="/home/fagg/datasets/radiant_earth/pa/", fold=args.fold, train_filt='*', cache_dir=args.cache, repeat_train=args.repeat, shuffle_train=args.shuffle, batch_size=args.batch, prefetch=args.prefetch, num_parallel_calls=args.num_parallel_calls)

    # Initialize lists to store true labels and predictions
    shallow_pred_labels = []
    deep_pred_labels = []
    true_labels = []

    # Store the true labels and predict on the data.
    for ins, outs, in ds_testing:
      # Predict on the data and store the max label preds.
      shallow_pred = np.argmax(shallow_models[i].predict(ins), axis = 3)
      deep_pred = np.argmax(deep_models[i].predict(ins), axis = 3)

      # Store the predictions.
      shallow_pred_labels.extend(shallow_pred.flatten())
      deep_pred_labels.extend(deep_pred.flatten())
      true_labels.extend(outs.numpy().flatten())
    
    # Convert to numpy.
    shallow_pred_labels = np.array(shallow_pred_labels)
    deep_pred_labels = np.array(deep_pred_labels)
    true_labels = np.array(true_labels)

  # Create confusion matrices
  shallow_cm = confusion_matrix(true_labels, shallow_pred_labels, labels=range(7))
  deep_cm = confusion_matrix(true_labels, deep_pred_labels, labels=range(7))
  
  # Plot the first confusion matrix.
  shallow_disp = ConfusionMatrixDisplay(
    confusion_matrix=shallow_cm, display_labels=classes
  )
  shallow_disp.plot(cmap='Blues', xticks_rotation="vertical")
  plt.title("3a: Shallow Model Confusion Matrix")
  plt.savefig("imgs/figure_3a")

  deep_disp = ConfusionMatrixDisplay(
    confusion_matrix=deep_cm, display_labels=classes
  )
  deep_disp.plot(cmap='Reds', xticks_rotation="vertical")
  plt.title("3b: Deep Model Confusion Matrix")
  plt.savefig("imgs/figure_3b")

def figure4(shallow_data, deep_data):
  '''
  Create a scatter plot of test set accuracy for deep vs. shallow networks.
  '''

  # Plot the figure.
  fig = plt.figure()
  for i in range(5):
    plt.scatter(
      shallow_data[i]['predict_testing_eval'][1],
      deep_data[i]['predict_testing_eval'][1],
      label=f"Fold:{i}"
    )
  
  plt.xlabel('Shallow Accuracy')
  plt.ylabel('Deep Accuracy')
  plt.axline((0,0), slope=1)
  plt.xlim(0, 1)
  plt.ylim(0, 1)
  plt.title('Deep vs. Shallow Test Accuracy')
  plt.legend()
  plt.savefig('imgs/figure_4')

def figure5(args, shallow_model, deep_model):
  '''
  Show ten interesting examples (one per row) for both models.

  Each row includes three columns (satelitte, true, predicted).
  '''
  # Load dataset and corresponding models
  _, _, ds_testing, _ = create_datasets(base_dir="/home/fagg/datasets/radiant_earth/pa/", fold=args.fold, train_filt='*', cache_dir=args.cache, repeat_train=args.repeat, shuffle_train=args.shuffle, batch_size=args.batch, prefetch=args.prefetch, num_parallel_calls=args.num_parallel_calls)

  # Choose 10 random images from the dataset.
  pool = iter(ds_testing.unbatch())

  # Create plot 5a for the shallow model.
  fig, axes = plt.subplots(
    10, 3, figsize=(6,24), constrained_layout=True
  )
  axes.flatten()
  plt.suptitle('Shallow Model Examples', fontsize=16)

  # for (axe, (img, label)) in zip(axes, pool):
  #   # Turn off the axis ticks.
  #   axe[0].axis('off')
  #   axe[1].axis('off')
  #   axe[2].axis('off')

  #   # Predict on the data.
  #   predicted = np.argmax(
  #     shallow_model.predict(img[np.newaxis, ...])[0],
  #     axis=-1
  #   )

  #   # Plot the satelite image (leftmost).
  #   axe[0].imshow(img[:, :, :3])
  #   axe[0].set_title('Satelite Img')

  #   # Plot the true labels.
  #   axe[1].imshow(label, vmax=6)
  #   axe[1].set_title('True Label')

  #   # Plot the predicted labels.
  #   axe[2].imshow(predicted, vmax=6)
  #   axe[2].set_title('Predicted Label')

  # plt.savefig('imgs/figure_5a.png', bbox_inches='tight')

  # # Create a plot, repeating the above, for 5b.
  # fig, axes = plt.subplots(
  #   10, 3, figsize=(6,24), constrained_layout=True
  # )
  # axes.flatten()
  # plt.suptitle('Deep Model Examples', fontsize=16)

  for (axe, (img, label)) in zip(axes, pool):
    # Turn off the axis ticks.
    axe[0].axis('off')
    axe[1].axis('off')
    axe[2].axis('off')

    # Predict on the data.
    predicted = np.argmax(
      deep_model.predict(img[np.newaxis, ...])[0],
      axis=-1
    )

    # Plot the satelite image (leftmost).
    axe[0].imshow(img[:, :, :3])
    axe[0].set_title('Satelite Img')

    # Plot the true labels.
    axe[1].imshow(label, vmax=6)
    axe[1].set_title('True Label')

    # Plot the predicted labels.
    axe[2].imshow(predicted, vmax=6)
    axe[2].set_title('Predicted Label')

  plt.savefig('imgs/figure_5b.png', bbox_inches='tight')


if __name__ == '__main__':

  # Store the different directories.
  home_dir = "/home/cs504310/hw4/results/"
  scratch_dir = "/scratch/cs504310/results/"

  shallow_file_names = [
    f"results_Net_Shallow___Csize3_Cfilters8_steps1_sdrop_sdrop_0.150__reg_L2_0.000100__lrate_LR_0.001000__fold_{i}_"
    for i in range(5)
  ]  # This creates a flat list

  deep_file_names = [
    f"results_Net_Deep___Csize3_Cfilters30_steps2_sdrop_sdrop_0.150__reg_L2_0.000100__lrate_LR_0.001000__fold_{i}_"
    for i in range(5)
  ]
  # Store the class labels.
  labels = ["No Class", "Water", "Tree Canopy/Forest", "Low Vegetation/Field", "Barren Land", "Impervious (Other)", "Impervious (Road)"]

  # Load in the shallow and deep data.
  shallow_data, shallow_models = [], []
  deep_data, deep_models = [], []

  for shallow_file, deep_file in zip(shallow_file_names, deep_file_names):
    # Open the shallow data and append.
    with open(scratch_dir + shallow_file + "results.pkl", "rb") as fp:
      shallow_data.append(pickle.load(fp))
    shallow_models.append(keras.saving.load_model(home_dir + shallow_file + "model.keras"))
  
    # Open the deep data and append.
    with open(scratch_dir + deep_file + "results.pkl", "rb") as fp:
      deep_data.append(pickle.load(fp))
    deep_models.append(keras.saving.load_model(home_dir + deep_file + "model.keras"))

    # figure1() included via --render flag.
  # figure2(shallow_data, deep_data)
  # figure3(shallow_data, deep_data, shallow_models, deep_models, labels)
  # figure4(shallow_data, deep_data)
  figure5(deep_data[2]['args'], shallow_models[2], deep_models[2])