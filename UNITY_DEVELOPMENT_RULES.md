# Directives de Développement Unity (Strict)

## 1. Philosophie et Style de Code
- **Principe KISS** : Prioriser la simplicité. Pas de sur-ingénierie.
- **Standard C#** : Appliquer la nomenclature Microsoft CoreFX. Accolades style Allman, indentation de 4 espaces.
- **Nomenclature** : Variables privées en `_camelCase`, visibilité explicite pour chaque membre, utilisation des mots-clés du langage (`int`, `string`) au lieu des types BCL (`Int32`, `String`).
- **Commentaires** : 
    - Documentation XML `/// <summary>` systématique pour chaque classe, méthode et variable exposée.
    - Commentaires de corps de méthode en anglais (niveau B1).
- **Interdiction** : Ne jamais supprimer les commentaires existants lors d'une modification.

## 2. Gestion de la Mémoire et Traçabilité
- **Journal de bord (Memory Logging)** : Pour chaque modification, mettre à jour `DEVELOPMENT_LOG.md` incluant :
    - Le code modifié/ajouté.
    - La justification technique (Pourquoi ?).
    - Le problème résolu (Fix) ou la fonctionnalité ajoutée.
- **Documentation technique** : Maintenir une documentation ultra-détaillée des fonctionnalités actuelles, déduite de l'analyse du code, pour garantir une continuité parfaite.

## 3. Qualité Production Unity
- **Optimisation** : Code performant, respect des principes SSOT (Single Source of Truth).
- **Observabilité** : Insérer des `Debug.Log` stratégiques pour permettre l'inspection du comportement à l'exécution.
- **Rigueur** : Ne jamais modifier ou supprimer de code hors du périmètre de la demande. Être minutieux et précis.

## 4. Communication
- **Direct** : Aller droit au but. Pas de politesses inutiles ou de remplissage.
- **Sources** : Fournir des liens vers la documentation officielle Unity ou Microsoft pour toute explication technique.

## 5. Workflow d'Exécution (Step-by-Step)
- **Planification (To-Do List)** : Avant chaque modification, établir et afficher une liste de tâches précise (To-Do List) détaillant les étapes techniques prévues.
- **Validation Progressive** : Exécuter les tâches une par une. À chaque étape, vérifier l'absence d'erreurs de syntaxe ou de régressions.
- **Vérification de Stabilité** : Avant de valider la réponse finale, s'assurer que le code est fonctionnel, compile sans erreur et respecte les contraintes Unity.
- **Auto-Correction** : Si une erreur est détectée, la signaler, expliquer la cause et corriger avant de poursuivre la liste.