"""
Generates accuracy table averaged across 3 evaluation runs of each LLM classifier.

Table structure:
  Rows: P1-P8 (prompt conditions) + Average row
  Cols: 7 emotions + Average per condition

P1 = Situation, NoGaze, NoUnilateral  = situation + NoGaze_NoAsym
P2 = Situation, NoGaze, Unilateral    = situation + NoGaze_Asym
P3 = Situation, Gaze,   NoUnilateral  = situation + Gaze_NoAsym
P4 = Situation, Gaze,   Unilateral    = situation + Gaze_Asym
P5 = Explicit,  NoGaze, NoUnilateral  = explicit  + NoGaze_NoAsym
P6 = Explicit,  NoGaze, Unilateral    = explicit  + NoGaze_Asym
P7 = Explicit,  Gaze,   NoUnilateral  = explicit  + Gaze_NoAsym
P8 = Explicit,  Gaze,   Unilateral    = explicit  + Gaze_Asym

"""

import os
import pathlib
import pandas as pd
import numpy as np
import matplotlib.pyplot as plt
import matplotlib
matplotlib.use('Agg')

# ── Config ────────────────────────────────────────────────────────────────────

EVAL_FOLDER = os.path.join(pathlib.Path.home(), "Desktop", "Evaluation_Run")
OUTPUT_DIR  = os.path.join(EVAL_FOLDER, "Tables")
os.makedirs(OUTPUT_DIR, exist_ok=True)

# ── Change these paths to your actual CSV files ───────────────────────────────

CSV_FILES_GEMINI25PRO = [
    os.path.join(EVAL_FOLDER, "evaluation_results1_gemini2.5pro.csv"),
    os.path.join(EVAL_FOLDER, "evaluation_results2_gemini2.5pro.csv"),
    os.path.join(EVAL_FOLDER, "evaluation_results3_gemini2.5pro.csv"),
]

CSV_FILES_GEMINI3FLASH = [
    os.path.join(EVAL_FOLDER, "evaluation_results1_gemini3flash.csv"),
    os.path.join(EVAL_FOLDER, "evaluation_results2_gemini3flash.csv"),
    os.path.join(EVAL_FOLDER, "evaluation_results3_gemini3flash.csv"),
]

# ── Prompt mapping ────────────────────────────────────────────────────────────

PROMPTS = {
    "P1": {"prompt_type": "situation", "condition": "NoGaze_NoAsym"},
    "P2": {"prompt_type": "situation", "condition": "NoGaze_Asym"},
    "P3": {"prompt_type": "situation", "condition": "Gaze_NoAsym"},
    "P4": {"prompt_type": "situation", "condition": "Gaze_Asym"},
    "P5": {"prompt_type": "explicit",  "condition": "NoGaze_NoAsym"},
    "P6": {"prompt_type": "explicit",  "condition": "NoGaze_Asym"},
    "P7": {"prompt_type": "explicit",  "condition": "Gaze_NoAsym"},
    "P8": {"prompt_type": "explicit",  "condition": "Gaze_Asym"},
}

EMOTIONS = ["anger", "contempt", "disgust", "fear", "happiness", "sadness", "surprise"]

# ── Core function ─────────────────────────────────────────────────────────────

def build_table(csv_files, label):
    print(f"\n=== Building table for {label} ===")

    # Load and combine all runs
    dfs = []
    for i, path in enumerate(csv_files):
        df = pd.read_csv(path)
        df["run"] = i + 1
        df["prompt_type"]      = df["prompt_type"].str.lower().str.strip()
        df["condition"]        = df["condition"].str.strip()
        df["intended_emotion"] = df["intended_emotion"].str.lower().str.strip()
        df["correct"]          = pd.to_numeric(df["correct"], errors="coerce")
        dfs.append(df)
        print(f"  Loaded run {i+1}: {len(df)} rows, accuracy={df['correct'].mean()*100:.1f}%")

    combined = pd.concat(dfs, ignore_index=True)
    print(f"  Combined: {len(combined)} rows")

    # Build table
    rows = []
    for prompt_key, mapping in PROMPTS.items():
        row = {"Prompt": prompt_key}
        subset = combined[
            (combined["prompt_type"] == mapping["prompt_type"]) &
            (combined["condition"]   == mapping["condition"])
        ]
        for emotion in EMOTIONS:
            e_subset = subset[subset["intended_emotion"] == emotion]
            acc = e_subset["correct"].mean() * 100 if len(e_subset) > 0 else 0
            row[emotion.capitalize()] = round(acc, 1)

        # Average per condition
        row["Average"] = round(np.mean([row[e.capitalize()] for e in EMOTIONS]), 1)
        rows.append(row)

    # Average row across all prompts
    avg_row = {"Prompt": "Average"}
    for emotion in EMOTIONS:
        avg_row[emotion.capitalize()] = round(np.mean([r[emotion.capitalize()] for r in rows]), 1)
    avg_row["Average"] = round(np.mean([r["Average"] for r in rows]), 1)
    rows.append(avg_row)

    table_df = pd.DataFrame(rows)
    table_df = table_df.set_index("Prompt")

    # Print table
    print(f"\n{label} — Accuracy Table (averaged across 3 runs):")
    print(table_df.to_string())

    # Save as CSV
    csv_path = os.path.join(OUTPUT_DIR, f"table_{label.replace(' ', '_').replace('.', '')}.csv")
    table_df.to_csv(csv_path)
    print(f"\nSaved CSV: {csv_path}")

    # Save as figure
    fig, ax = plt.subplots(figsize=(14, 6))
    ax.axis("off")

    col_labels = ["Prompt"] + list(table_df.columns)
    row_labels = list(table_df.index)
    cell_data  = [[idx] + [f"{v:.1f}%" for v in row] for idx, row in zip(row_labels, table_df.values)]

    table = ax.table(
        cellText=cell_data,
        colLabels=col_labels,
        loc="center",
        cellLoc="center"
    )
    table.auto_set_font_size(False)
    table.set_fontsize(9)
    table.scale(1.2, 1.8)

    # Color header
    for j in range(len(col_labels)):
        table[0, j].set_facecolor("#4285F4")
        table[0, j].set_text_props(color="white", fontweight="bold")

    # Color average row
    for j in range(len(col_labels)):
        table[len(row_labels), j].set_facecolor("#E8F0FE")
        table[len(row_labels), j].set_text_props(fontweight="bold")

    # Color average column
    avg_col_idx = len(col_labels) - 1
    for i in range(1, len(row_labels) + 1):
        table[i, avg_col_idx].set_facecolor("#FFF3E0")
        table[i, avg_col_idx].set_text_props(fontweight="bold")

    ax.set_title(f"Emotion Recognition Accuracy — {label}\n(Average of 3 Evaluation Runs)",
                 fontsize=13, fontweight="bold", pad=20)

    plt.tight_layout()
    fig_path = os.path.join(OUTPUT_DIR, f"table_{label.replace(' ', '_').replace('.', '')}.png")
    fig.savefig(fig_path, dpi=150, bbox_inches="tight")
    plt.close(fig)
    print(f"Saved figure: {fig_path}")

    return table_df

# ── Run ───────────────────────────────────────────────────────────────────────

table_25pro   = build_table(CSV_FILES_GEMINI25PRO,   "Gemini 2.5 Pro")
table_3flash  = build_table(CSV_FILES_GEMINI3FLASH,  "Gemini 3 Flash")

print("\n" + "─" * 60)
print(f"All tables saved to: {OUTPUT_DIR}")
