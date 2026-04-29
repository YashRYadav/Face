"""
misclassification_analysis.py
==============================
Analyzes what each intended emotion was classified as
by Gemini 2.5 Pro and Gemini 3 Flash classifiers and generates confusion matrices.

Usage:
  py -3.13 misclassification_analysis.py

Output: Desktop/Evaluation_Run/Misclassification_Classifier_LLMs/
"""

import os
import pathlib
import pandas as pd
import numpy as np
import matplotlib.pyplot as plt
import matplotlib
matplotlib.use('Agg')

EVAL_FOLDER = os.path.join(pathlib.Path.home(), "Desktop", "Evaluation_Run")
OUTPUT_DIR  = os.path.join(EVAL_FOLDER, "Misclassification")
os.makedirs(OUTPUT_DIR, exist_ok=True)

CSV_FILES = {
    "Gemini 2.5 Pro": [
        os.path.join(EVAL_FOLDER, "evaluation_results1_gemini2.5pro.csv"),
        os.path.join(EVAL_FOLDER, "evaluation_results2_gemini2.5pro.csv"),
        os.path.join(EVAL_FOLDER, "evaluation_results3_gemini2.5pro.csv"),
    ],
    "Gemini 3 Flash": [
        os.path.join(EVAL_FOLDER, "evaluation_results1_gemini3flash.csv"),
        os.path.join(EVAL_FOLDER, "evaluation_results2_gemini3flash.csv"),
        os.path.join(EVAL_FOLDER, "evaluation_results3_gemini3flash.csv"),
    ]
}

EMOTIONS = ["anger", "contempt", "disgust", "fear", "happiness", "sadness", "surprise"]


def analyze(csv_files, name):
    print(f"\n=== {name} ===")
    dfs = []
    for path in csv_files:
        df = pd.read_csv(path)
        df["intended_emotion"]  = df["intended_emotion"].str.lower().str.strip()
        df["predicted_emotion"] = df["predicted_emotion"].str.lower().str.strip()
        dfs.append(df)
    combined = pd.concat(dfs, ignore_index=True)

    confusion = pd.DataFrame(0.0, index=EMOTIONS, columns=EMOTIONS + ["error"])
    for _, row in combined.iterrows():
        intended  = row["intended_emotion"]
        predicted = row["predicted_emotion"]
        if intended in EMOTIONS:
            if predicted in EMOTIONS:
                confusion.loc[intended, predicted] += 1
            else:
                confusion.loc[intended, "error"] += 1

    confusion_pct = confusion.copy()
    for emotion in EMOTIONS:
        total = confusion.loc[emotion].sum()
        if total > 0:
            confusion_pct.loc[emotion] = (confusion.loc[emotion] / total * 100).round(1)

    print(f"\n{'Intended':<12}", end="")
    for e in EMOTIONS:
        print(f"{e.capitalize()[:7]:>9}", end="")
    print(f"{'Error':>9}")
    print("-" * 80)
    for intended in EMOTIONS:
        print(f"{intended.capitalize():<12}", end="")
        for predicted in EMOTIONS:
            val = confusion_pct.loc[intended, predicted]
            marker = "*" if intended == predicted else " "
            print(f"{val:>8.1f}{marker}", end="")
        print(f"{confusion_pct.loc[intended, 'error']:>9.1f}")

    print(f"\nMisclassification summary:")
    for intended in EMOTIONS:
        correct_pct = confusion_pct.loc[intended, intended]
        misclass = confusion_pct.loc[intended].drop([intended, "error"]).sort_values(ascending=False)
        top = misclass[misclass > 0].head(3)
        top_str = ", ".join([f"{e}={v:.1f}%" for e, v in top.items()]) if len(top) > 0 else "none"
        print(f"  {intended.capitalize():<12}: correct={correct_pct:.1f}% | misclassified as: {top_str}")

    fig, ax = plt.subplots(figsize=(10, 8))
    plot_data = confusion_pct[EMOTIONS]
    im = ax.imshow(plot_data.values, cmap="Blues", aspect="auto", vmin=0, vmax=100)
    plt.colorbar(im, ax=ax, label="Classification %")
    ax.set_xticks(range(len(EMOTIONS)))
    ax.set_yticks(range(len(EMOTIONS)))
    ax.set_xticklabels([e.capitalize() for e in EMOTIONS], rotation=45, ha="right", fontsize=10)
    ax.set_yticklabels([e.capitalize() for e in EMOTIONS], fontsize=10)
    ax.set_xlabel("Predicted Emotion", fontsize=12)
    ax.set_ylabel("Intended Emotion", fontsize=12)

    for i in range(len(EMOTIONS)):
        for j in range(len(EMOTIONS)):
            val = plot_data.values[i, j]
            color = "white" if val > 60 else "black"
            weight = "bold" if i == j else "normal"
            ax.text(j, i, f"{val:.1f}", ha="center", va="center",
                    fontsize=9, color=color, fontweight=weight)
    plt.tight_layout()
    safe_name = name.replace(" ", "_").replace(".", "")
    path = os.path.join(OUTPUT_DIR, f"confusion_{safe_name}2.png")
    fig.savefig(path, dpi=150, bbox_inches="tight")
    plt.close(fig)
    print(f"Saved: {path}")


for name, files in CSV_FILES.items():
    analyze(files, name)

print(f"\nDone. Saved to: {OUTPUT_DIR}")
