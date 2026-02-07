<script>
    import { createEventDispatcher, onMount } from "svelte";
    import { fly } from "svelte/transition";
    import { cubicOut } from "svelte/easing";
    import { api } from "../lib/api.js";

    const dispatch = createEventDispatcher();

    export let initialMode = "login";
    export let navigate;

    let mode = initialMode;
    let loading = false;
    let error = "";
    let visible = false;

    let email = "";
    let password = "";
    let fullName = "";
    let nationalIdNumber = "";
    let idCardFront = null;
    let idCardFrontName = "";
    let idCardBack = null;
    let idCardBackName = "";

    onMount(() => {
        visible = true;
    });

    $: mode = initialMode;

    function handleFrontChange(e) {
        const file = e.target.files[0];
        if (file) {
            idCardFront = file;
            idCardFrontName = file.name;
        }
    }

    function handleBackChange(e) {
        const file = e.target.files[0];
        if (file) {
            idCardBack = file;
            idCardBackName = file.name;
        }
    }

    async function handleSubmit() {
        loading = true;
        error = "";
        try {
            let result;
            if (mode === "register") {
                if (!idCardFront || !idCardBack) {
                    error =
                        "Please upload both front and back photos of your national ID card";
                    loading = false;
                    return;
                }
                result = await api.register({
                    fullName,
                    email,
                    password,
                    idCardFront,
                    idCardBack,
                    nationalIdNumber,
                });
            } else {
                result = await api.login({ email, password });
            }
            dispatch("login", { token: result.token, user: result.user });
        } catch (err) {
            error = err.message || "Authentication failed";
        } finally {
            loading = false;
        }
    }

    function switchMode(newMode) {
        mode = newMode;
        error = "";
        if (navigate) navigate(newMode);
    }
</script>

<main class="atomic-app">
    <!-- Floating Background Atoms -->
    <div class="atom atom-1"></div>
    <div class="atom atom-2"></div>
    <div class="atom atom-3"></div>
    <div class="atom atom-4"></div>
    <div class="atom atom-5"></div>

    {#if visible}
        <section
            class="auth-section"
            in:fly={{ y: 20, duration: 400, delay: 0, easing: cubicOut }}
        >
            <div class="glass-panel auth-card">
                <h1 class="headline">
                    {#if mode === "login"}
                        <span class="highlight">Welcome</span> Back
                    {:else}
                        <span class="highlight">Create</span> Account
                    {/if}
                </h1>

                {#if error}
                    <div class="error-msg">{error}</div>
                {/if}

                <form on:submit|preventDefault={handleSubmit}>
                    {#if mode === "register"}
                        <div class="input-group">
                            <label for="fullname">Full Name</label>
                            <input
                                type="text"
                                id="fullname"
                                placeholder="Enter your full name"
                                bind:value={fullName}
                                required
                                minlength="2"
                                maxlength="100"
                            />
                        </div>
                    {/if}

                    <div class="input-group">
                        <label for="email">Email</label>
                        <input
                            type="email"
                            id="email"
                            placeholder="Enter your email"
                            bind:value={email}
                            required
                        />
                    </div>

                    <div class="input-group">
                        <label for="password">Password</label>
                        <input
                            type="password"
                            id="password"
                            placeholder={mode === "login"
                                ? "Enter your password"
                                : "Create a password (min 8 chars)"}
                            bind:value={password}
                            required
                            minlength={mode === "register" ? 8 : undefined}
                        />
                    </div>

                    {#if mode === "register"}
                        <div class="input-group">
                            <label for="nationalIdNumber"
                                >National ID Number</label
                            >
                            <input
                                type="text"
                                id="nationalIdNumber"
                                placeholder="e.g. AB123456"
                                bind:value={nationalIdNumber}
                                required
                                maxlength="50"
                            />
                        </div>

                        <div class="id-upload-section">
                            <label class="section-label"
                                >National ID Verification</label
                            >
                            <div class="upload-grid">
                                <div
                                    class="upload-box"
                                    class:has-file={idCardFrontName}
                                >
                                    <span class="upload-icon">&#128179;</span>
                                    <span class="upload-text">
                                        {idCardFrontName || "Front Side"}
                                    </span>
                                    <input
                                        type="file"
                                        accept="image/jpeg,image/png,image/webp"
                                        on:change={handleFrontChange}
                                        required
                                    />
                                </div>
                                <div
                                    class="upload-box"
                                    class:has-file={idCardBackName}
                                >
                                    <span class="upload-icon">&#128283;</span>
                                    <span class="upload-text">
                                        {idCardBackName || "Back Side"}
                                    </span>
                                    <input
                                        type="file"
                                        accept="image/jpeg,image/png,image/webp"
                                        on:change={handleBackChange}
                                        required
                                    />
                                </div>
                            </div>
                            <p class="upload-hint">
                                An admin will review your ID for verification
                                before activating your account.
                            </p>
                        </div>
                    {/if}

                    <button
                        class="cta-button primary"
                        type="submit"
                        disabled={loading}
                    >
                        <span class="btn-text">
                            {#if loading}Processing...{:else}{mode === "login"
                                    ? "Login"
                                    : "Register"}{/if}
                        </span>
                    </button>

                    <button
                        class="switch-link"
                        type="button"
                        on:click={() =>
                            switchMode(mode === "login" ? "register" : "login")}
                    >
                        <span class="switch-text">
                            {mode === "login"
                                ? "Don't have an account?"
                                : "Already have an account?"}
                        </span>
                        {mode === "login" ? "Register" : "Login"}
                    </button>
                </form>

                <button class="back-link" on:click={() => navigate("home")}>
                    &larr; Back to Home
                </button>
            </div>
        </section>
    {/if}
</main>

<style>
    .atomic-app {
        position: relative;
        min-height: 100vh;
        width: 100%;
        overflow-y: auto;
        overflow-x: hidden;
        background: var(--bg-secondary, #ecf0f1);
        display: flex;
        justify-content: center;
        align-items: center;
        padding: 2rem 1rem;
    }

    /* TYPOGRAPHY */
    h1,
    label,
    input,
    span {
        color: var(--text-main, #2c3e50);
    }

    .headline {
        font-size: 2.2rem;
        font-weight: 700;
        letter-spacing: -1px;
        margin-bottom: 1.5rem;
        text-align: center;
        margin-top: 0;
        color: var(--brand-dark, #34495e);
    }

    .highlight {
        background: linear-gradient(
            135deg,
            var(--brand-primary, #3498db) 0%,
            var(--accent, #f8c957) 100%
        );
        -webkit-background-clip: text;
        -webkit-text-fill-color: transparent;
    }

    /* GLASSMORPHISM - white style */
    .glass-panel {
        background: rgba(255, 255, 255, 0.85);
        backdrop-filter: blur(25px);
        -webkit-backdrop-filter: blur(25px);
        border: 1px solid rgba(255, 255, 255, 0.6);
        border-radius: 20px;
        padding: 2.5rem;
        box-shadow: 0 8px 32px rgba(0, 0, 0, 0.08);
        width: 100%;
        max-width: 420px;
        position: relative;
        overflow: hidden;
    }

    /* INPUTS */
    .input-group {
        margin-bottom: 1rem;
        text-align: left;
    }

    label {
        display: block;
        margin-bottom: 0.5rem;
        font-size: 0.9rem;
        color: var(--text-muted, #7f8c8d);
    }

    input[type="text"],
    input[type="email"],
    input[type="password"] {
        width: 100%;
        padding: 0.8rem 1rem;
        background: white;
        border: 1px solid #e2e8f0;
        border-radius: 12px;
        color: var(--text-main, #2c3e50);
        font-family: inherit;
        font-size: 0.95rem;
        transition: all 0.3s ease;
        box-sizing: border-box;
    }

    input:focus {
        outline: none;
        background: white;
        border-color: var(--brand-primary, #3498db);
        box-shadow: 0 0 0 3px rgba(52, 152, 219, 0.15);
    }

    input::placeholder {
        color: #b0bec5;
    }

    /* UPLOAD SECTION */
    .id-upload-section {
        margin-bottom: 1.5rem;
        text-align: left;
    }

    .section-label {
        font-size: 0.9rem;
        color: var(--text-muted, #7f8c8d);
        margin-bottom: 0.8rem;
        display: block;
    }

    .upload-grid {
        display: grid;
        grid-template-columns: 1fr 1fr;
        gap: 1rem;
    }

    .upload-box {
        position: relative;
        background: white;
        border: 2px dashed #d1d5db;
        border-radius: 10px;
        padding: 0.5rem;
        text-align: center;
        transition: all 0.3s ease;
        display: flex;
        flex-direction: column;
        align-items: center;
        justify-content: center;
        height: 70px;
        cursor: pointer;
    }

    .upload-box:hover {
        background: #f0f7ff;
        border-color: var(--brand-primary, #3498db);
    }

    .upload-box.has-file {
        border-color: #27ae60;
        background: rgba(39, 174, 96, 0.05);
    }

    .upload-icon {
        font-size: 1.2rem;
        margin-bottom: 0.2rem;
    }

    .upload-text {
        font-size: 0.75rem;
        color: var(--text-muted, #7f8c8d);
        max-width: 100%;
        overflow: hidden;
        text-overflow: ellipsis;
        white-space: nowrap;
    }

    .upload-box input[type="file"] {
        position: absolute;
        top: 0;
        left: 0;
        width: 100%;
        height: 100%;
        opacity: 0;
        cursor: pointer;
    }

    .upload-hint {
        font-size: 0.7rem;
        color: var(--text-muted, #7f8c8d);
        margin-top: 0.5rem;
    }

    /* ERROR */
    .error-msg {
        background: #fef2f2;
        border: 1px solid #fecaca;
        color: #dc2626;
        padding: 0.7rem 1rem;
        border-radius: 10px;
        font-size: 0.85rem;
        margin-bottom: 1rem;
        text-align: center;
    }

    /* BUTTONS */
    .cta-button {
        position: relative;
        padding: 0.9rem 2rem;
        background: var(--brand-dark, #34495e);
        border: none;
        border-radius: 50px;
        color: #fff;
        font-family: inherit;
        font-weight: 600;
        font-size: 1rem;
        cursor: pointer;
        overflow: hidden;
        transition: all 0.3s ease;
        width: 100%;
        margin-top: 0.5rem;
    }

    .cta-button:hover {
        background: var(--brand-primary, #3498db);
        transform: scale(1.02);
        box-shadow: 0 4px 15px rgba(52, 152, 219, 0.3);
    }

    .cta-button:disabled {
        opacity: 0.6;
        cursor: not-allowed;
        transform: none;
    }

    .cta-button.primary {
        background: var(--brand-dark, #34495e);
    }

    .btn-text {
        background: linear-gradient(
            135deg,
            #d1d5db 0%,
            #ffffff 50%,
            #e5e7eb 100%
        );
        -webkit-background-clip: text;
        -webkit-text-fill-color: transparent;
        background-clip: text;
    }

    /* ANIMATED BORDER GRADIENT */
    .glass-panel::before {
        content: "";
        position: absolute;
        top: 0;
        left: 0;
        right: 0;
        bottom: 0;
        border-radius: inherit;
        padding: 1.5px;
        background: linear-gradient(
            90deg,
            var(--brand-primary, #3498db),
            var(--accent, #f8c957),
            #27ae60,
            var(--brand-primary, #3498db)
        );
        background-size: 300% 300%;
        -webkit-mask:
            linear-gradient(#fff 0 0) content-box,
            linear-gradient(#fff 0 0);
        -webkit-mask-composite: xor;
        mask-composite: exclude;
        animation: borderRotate 4s linear infinite;
        pointer-events: none;
    }

    @keyframes borderRotate {
        0% {
            background-position: 0% 50%;
        }
        100% {
            background-position: 100% 50%;
        }
    }

    /* LINKS */
    .switch-link {
        background: none;
        border: none;
        color: var(--brand-primary, #3498db);
        text-decoration: underline;
        cursor: pointer;
        font-family: inherit;
        font-size: 0.9rem;
        margin-top: 1rem;
        display: block;
        width: 100%;
        text-align: center;
        opacity: 0.9;
        transition: opacity 0.3s;
    }

    .switch-link:hover {
        opacity: 1;
    }

    .switch-text {
        color: var(--text-muted, #7f8c8d);
        text-decoration: none;
        display: inline-block;
        margin-right: 4px;
        cursor: default;
    }

    .back-link {
        background: none;
        border: none;
        color: var(--text-muted, #7f8c8d);
        cursor: pointer;
        font-family: inherit;
        font-size: 0.9rem;
        margin-top: 2rem;
        transition: color 0.3s;
    }

    .back-link:hover {
        color: var(--brand-primary, #3498db);
    }

    /* FLOATING ATOMS - subtle light blue/gold blobs */
    .atom {
        position: absolute;
        border-radius: 50%;
        filter: blur(100px);
        opacity: 0.25;
        z-index: 0;
        pointer-events: none;
        animation: floatAtom 15s infinite ease-in-out alternate;
    }

    .atom-1 {
        width: 400px;
        height: 400px;
        background: var(--brand-primary, #3498db);
        top: -10%;
        left: -10%;
        animation-delay: 0s;
    }
    .atom-2 {
        width: 500px;
        height: 500px;
        background: #85c1e9;
        bottom: 10%;
        right: -10%;
        animation-delay: -5s;
    }
    .atom-3 {
        width: 300px;
        height: 300px;
        background: var(--accent, #f8c957);
        top: 40%;
        left: 30%;
        opacity: 0.15;
        animation-delay: -10s;
    }
    .atom-4 {
        width: 200px;
        height: 200px;
        background: #aed6f1;
        top: 15%;
        right: 15%;
        opacity: 0.2;
        animation-delay: -2s;
    }
    .atom-5 {
        width: 350px;
        height: 350px;
        background: var(--accent, #f8c957);
        bottom: 5%;
        left: 10%;
        opacity: 0.12;
        animation-delay: -7s;
    }

    @keyframes floatAtom {
        0% {
            transform: translate(0, 0) scale(1);
        }
        100% {
            transform: translate(30px, -30px) scale(1.1);
        }
    }

    .auth-section {
        position: relative;
        z-index: 1;
    }
</style>
