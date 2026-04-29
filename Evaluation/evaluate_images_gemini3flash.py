"""
evaluate_images_gemini31pro.py
==============================
Classifies all evaluation images using Gemini 3 flash Preview.
Output is plain text — parsed directly, no JSON.

Usage:
  pip install google-generativeai pillow
  python evaluate_images_gemini3flash.py
"""

import os
import csv
import time
import re
import pathlib
import google.generativeai as genai
from PIL import Image

# ── Config ────────────────────────────────────────────────────────────────────

GEMINI_API_KEY = "ENTER YOUR API KEY HERE"
MODEL_NAME     = "gemini-3-flash-preview"

EVAL_FOLDER = os.path.join(pathlib.Path.home(), "Desktop", "Evaluation_Run")
OUTPUT_CSV  = os.path.join(EVAL_FOLDER, "evaluation_results5_gemini3flash.csv")

VALID_EMOTIONS = ["anger", "contempt", "disgust", "fear", "happiness", "sadness", "surprise"]

API_DELAY = 0

# ── Gemini setup ──────────────────────────────────────────────────────────────

genai.configure(api_key=GEMINI_API_KEY)
model = genai.GenerativeModel(MODEL_NAME)

# ── Classification prompt ─────────────────────────────────────────────────────

CLASSIFICATION_PROMPT = """You are a FACS (Facial Action Coding System) expert evaluating facial expressions on an anime character.

Look at this image and:
1. Identify which of the 7 Ekman basic emotions is most clearly expressed: anger, contempt, disgust, fear, happiness, sadness, surprise
2. Give a confidence score from 1 to 5:
   5 = Expression is very clear and unambiguous
   4 = Expression is clear with minor ambiguity
   3 = Expression is recognizable but subtle
   2 = Expression is weak or partially visible
   1 = Expression is barely visible or neutral face
predicted_emotion must be exactly one of: anger, contempt, disgust, fear, happiness, sadness, surprise
confidence must be 1, 2, 3, 4, or 5
Output only predicted emotion and confidence score.
"""

# ── Image classification ──────────────────────────────────────────────────────

def classify_image(image_path):
    try:
        img = Image.open(image_path)
        response = model.generate_content([CLASSIFICATION_PROMPT, img])

        content = response.text.strip().lower()

        # Extract emotion — find first valid emotion word in response
        predicted = None
        for emotion in VALID_EMOTIONS:
            if emotion in content:
                predicted = emotion
                break

        if predicted is None:
            print(f"  WARNING: No valid emotion found in output: '{content}'")
            return {"predicted_emotion": "error", "confidence": 0, "notes": f"raw: {content}"}

        # Extract confidence — find first digit 1-5 in response
        confidence = 0
        match = re.search(r"\b([1-5])\b", content)
        if match:
            confidence = int(match.group(1))

        return {"predicted_emotion": predicted, "confidence": confidence, "notes": ""}

    except Exception as e:
        print(f"  ERROR classifying {image_path}: {e}")
        return {"predicted_emotion": "error", "confidence": 0, "notes": str(e)}

# ── Parse filename ────────────────────────────────────────────────────────────

def parse_filename(filename):
    name  = filename.replace(".png", "")
    parts = name.split("_")
    try:
        return {
            "model":       parts[0],
            "prompt_type": parts[1],
            "condition":   parts[2] + "_" + parts[3],
            "emotion":     parts[4],
            "iteration":   parts[5]
        }
    except IndexError:
        return None

# ── Main ──────────────────────────────────────────────────────────────────────

def main():
    if not os.path.exists(EVAL_FOLDER):
        print(f"ERROR: Folder not found: {EVAL_FOLDER}")
        return

    image_paths = []
    for root, dirs, files in os.walk(EVAL_FOLDER):
        for file in files:
            if file.endswith(".png"):
                image_paths.append(os.path.join(root, file))

    image_paths.sort()
    total = len(image_paths)
    print(f"Found {total} images in {EVAL_FOLDER}")
    print(f"Results will be saved to: {OUTPUT_CSV}")
    print("─" * 60)

    headers = [
        "filename", "model", "prompt_type", "condition",
        "intended_emotion", "iteration", "predicted_emotion",
        "confidence", "correct", "notes"
    ]

    with open(OUTPUT_CSV, "w", newline="", encoding="utf-8") as csvfile:
        writer = csv.DictWriter(csvfile, fieldnames=headers)
        writer.writeheader()

        for idx, image_path in enumerate(image_paths):
            filename = os.path.basename(image_path)
            parsed   = parse_filename(filename)

            if parsed is None:
                print(f"[{idx+1}/{total}] SKIP (unparseable): {filename}")
                continue

            print(f"[{idx+1}/{total}] Classifying: {filename}")

            result    = classify_image(image_path)
            intended  = parsed["emotion"]
            predicted = result.get("predicted_emotion", "error")
            correct   = 1 if predicted == intended else 0

            row = {
                "filename":          filename,
                "model":             parsed["model"],
                "prompt_type":       parsed["prompt_type"],
                "condition":         parsed["condition"],
                "intended_emotion":  intended,
                "iteration":         parsed["iteration"],
                "predicted_emotion": predicted,
                "confidence":        result.get("confidence", 0),
                "correct":           correct,
                "notes":             result.get("notes", "")
            }

            writer.writerow(row)
            csvfile.flush()

            match = "✓" if correct else "✗"
            print(f"  {match} Intended: {intended} | Predicted: {predicted} | Confidence: {result.get('confidence', 0)}/5")

            time.sleep(API_DELAY)

    print("─" * 60)
    print(f"COMPLETE. Results saved to: {OUTPUT_CSV}")

if __name__ == "__main__":
    main()