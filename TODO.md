# Out-of-Nothing : Energy System Refactoring TODO

## Phase 1 : Analyse et Exploration [TERMINÉE]
- [x] Analyser l'architecture actuelle (Networks, Yellow Balls, Machines).
- [x] Étudier l'implémentation actuelle des Electric Arcs.
- [x] Localiser le bug des machines rouges (Red Materialisor).

## Phase 2 : Gestion Spatiale et Connexions [TERMINÉE]
- [x] Modifier la détection de connexion : passage en Edge-to-Edge (Somme des rayons).
- [x] Optimiser les Electric Arcs : calcul du Shortest Path entre circonférences.
- [x] Implémenter l'Update Dynamique des points d'ancrage en temps réel.

## Phase 3 : Correction de Bug & Précision Numérique [EN COURS]
- [ ] Fix : Identifier pourquoi les machines rouges ne se vident plus à 100%.
- [ ] Implémenter le clamping/arrondi intelligent par tick pour les floating points.
- [ ] Ajouter des flags de log granulaires (bool) par entité/système.

## Phase 4 : Tick Manager & Synchronisation
- [ ] Implémenter le groupement par Type & Network au sein du Tick Manager.
- [ ] Ajouter la logique de synchronisation automatique (Attente du prochain cycle).
- [ ] Implémenter les états latents (pas de consommation, feedback visuel atténué).
- [ ] Prévoir l'extensibilité pour l'offset manuel (Ctrl + Clic).

## Phase 5 : Debug & performance
- [ ] Optimisation Scalable (~500 réseaux) via HashSets et réduction d'itérations.
- [ ] Nettoyage final et journal de bord complet.
