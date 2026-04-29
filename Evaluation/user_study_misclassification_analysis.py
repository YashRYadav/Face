"""
user_study_confusion.py
========================
Generates confusion matrices from user study CSV file.
One matrix for each condition (NO gaze NO asym, gaze+ ASYM).

Usage:
  py -3.13 user_study_confusion.py

Output: Desktop/Evaluation_Run/Misclassification/
"""

import os
import pathlib
import pandas as pd
import numpy as np
import matplotlib.pyplot as plt
import matplotlib
matplotlib.use('Agg')

# ── Config ────────────────────────────────────────────────────────────────────

CSV_PATH   = os.path.join(pathlib.Path.home(), "Desktop", "emotion_recognition_summary_percentages.csv")
OUTPUT_DIR = os.path.join(pathlib.Path.home(), "Desktop", "Evaluation_Run", "Misclassification")
os.makedirs(OUTPUT_DIR, exist_ok=True)

EMOTIONS = ["Anger", "Contempt", "Disgust", "Fear", "Happiness", "Sadness", "Surprise"]

# ── Load data ─────────────────────────────────────────────────────────────────

df = pd.read_csv(CSV_PATH)
df = df.dropna(subset=["Condition", "True_Emotion", "Predicted_Emotion"])
df["Condition"]         = df["Condition"].str.strip()
df["True_Emotion"]      = df["True_Emotion"].str.strip()
df["Predicted_Emotion"] = df["Predicted_Emotion"].str.strip()
df["Percentage (%)"]    = pd.to_numeric(df["Percentage (%)"], errors="coerce")

conditions = df["Condition"].unique()
print(f"Conditions found: {conditions}")

# ── Build and plot confusion matrix per condition ─────────────────────────────

def build_confusion(df_cond, condition_name):
    confusion = pd.DataFrame(0.0, index=EMOTIONS, columns=EMOTIONS)

    for _, row in df_cond.iterrows():
        true_e = row["True_Emotion"]
        pred_e = row["Predicted_Emotion"]
        pct    = row["Percentage (%)"]
        if true_e in EMOTIONS and pred_e in EMOTIONS:
            confusion.loc[true_e, pred_e] = pct

    print(f"\n=== {condition_name} ===")
    print(f"{'Intended':<12}", end="")
    for e in EMOTIONS:
        print(f"{e[:7]:>9}", end="")
    print()
    print("-" * 80)
    for intended in EMOTIONS:
        print(f"{intended:<12}", end="")
        for predicted in EMOTIONS:
            val = confusion.loc[intended, predicted]
            marker = "*" if intended == predicted else " "
            print(f"{val:>8.1f}{marker}", end="")
        print()

    print(f"\nMisclassification summary:")
    for intended in EMOTIONS:
        correct = confusion.loc[intended, intended]
        misclass = confusion.loc[intended, [e for e in EMOTIONS if e != intended]].sort_values(ascending=False)
        top = misclass[misclass > 0].head(3)
        top_str = ", ".join([f"{e}={v:.1f}%" for e, v in top.items()]) if len(top) > 0 else "none"
        print(f"  {intended:<12}: correct={correct:.1f}% | misclassified as: {top_str}")

    # Plot
    fig, ax = plt.subplots(figsize=(10, 8))
    im = ax.imshow(confusion.values, cmap="Blues", aspect="auto", vmin=0, vmax=100)
    plt.colorbar(im, ax=ax, label="Classification %")
    ax.set_xticks(range(len(EMOTIONS)))
    ax.set_yticks(range(len(EMOTIONS)))
    ax.set_xticklabels(EMOTIONS, rotation=45, ha="right", fontsize=10)
    ax.set_yticklabels(EMOTIONS, fontsize=10)
    ax.set_xlabel("Predicted Emotion", fontsize=12)
    ax.set_ylabel("Intended Emotion", fontsize=12)
    for i in range(len(EMOTIONS)):
        for j in range(len(EMOTIONS)):
            val = confusion.values[i, j]
            color = "white" if val > 60 else "black"
            weight = "bold" if i == j else "normal"
            ax.text(j, i, f"{val:.1f}", ha="center", va="center",
                    fontsize=9, color=color, fontweight=weight)
    plt.tight_layout()
    safe_name = condition_name.replace(" ", "_").replace("+", "plus")
    path = os.path.join(OUTPUT_DIR, f"confusion_userstudy_{safe_name}2.png")
    fig.savefig(path, dpi=150, bbox_inches="tight")
    plt.close(fig)
    print(f"Saved: {path}")


for cond in conditions:
    df_cond = df[df["Condition"] == cond]
    build_confusion(df_cond, cond)

print(f"\nDone. Saved to: {OUTPUT_DIR}")